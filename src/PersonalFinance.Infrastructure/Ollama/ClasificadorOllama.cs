using System.Globalization;
using System.Text;
using System.Text.Json;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using PersonalFinance.Domain.Clasificacion;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Ollama;

/// <summary>
/// Adaptador de <see cref="IClasificador"/> sobre Ollama. Es el único tipo del repo que importa
/// <c>OllamaSharp</c>.
/// </summary>
public sealed class ClasificadorOllama : IClasificador
{
    /// <summary>Tope de la respuesta del modelo. Más que esto no es una clasificación.</summary>
    private const int MaximoRespuesta = 8 * 1024;

    /// <summary>Categoría de descarte del seed (FR-09).</summary>
    private const string Descarte = "Otros";

    private readonly IOllamaApiClient _cliente;
    private readonly OpcionesOllama _opciones;

    public ClasificadorOllama(IOllamaApiClient cliente, OpcionesOllama opciones)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        ArgumentNullException.ThrowIfNull(opciones);

        _cliente = cliente;
        _opciones = opciones;
    }

    public async Task<ResultadoClasificacion> ClasificarAsync(
        string texto,
        IReadOnlyList<Categoria> categoriasActivas,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(texto.Length, Mensaje.TextoMaximo, nameof(texto));
        ArgumentNullException.ThrowIfNull(categoriasActivas);

        if (categoriasActivas.Count == 0)
        {
            // Clasificar sin categorías es un error de programación, no un caso de negocio.
            throw new ArgumentException(
                "No se puede clasificar sin categorías activas.", nameof(categoriasActivas));
        }

        if (SinDescripcionUtilizable(texto))
        {
            // Se resuelve sobre el texto de entrada y antes de llamar al modelo. AC-10 habla del
            // texto que no describe nada —"$3.000" a secas—, y eso es una propiedad del input:
            // preguntárselo al modelo era medir si el modelo completa un campo, que es otra cosa.
            // De paso, ahorra la llamada.
            return new ResultadoClasificacion.SinDescripcion();
        }

        var respuesta = await PedirAsync(texto, categoriasActivas, cancellationToken);

        return respuesta is null
            ? new ResultadoClasificacion.NoDisponible()
            : Interpretar(respuesta, categoriasActivas);
    }

    /// <summary>
    /// Un texto sin una sola letra —sólo importe, símbolos y puntuación— no describe ningún
    /// movimiento: es un número suelto.
    /// </summary>
    private static bool SinDescripcionUtilizable(string texto) => !texto.Any(char.IsLetter);

    /// <summary>
    /// Una sola llamada al modelo, sin reintentos: reintentar comprometería el p90 de NFR-02.
    /// Devuelve <c>null</c> cuando Ollama no respondió, que el llamador traduce a
    /// <see cref="ResultadoClasificacion.NoDisponible"/>.
    /// </summary>
    private async Task<string?> PedirAsync(
        string texto,
        IReadOnlyList<Categoria> categoriasActivas,
        CancellationToken cancellationToken)
    {
        var pedido = new ChatRequest
        {
            Model = _opciones.Modelo,
            Messages = PromptClasificacion.Construir(texto, categoriasActivas),
            Format = EsquemaClasificacion.Crear(categoriasActivas),
            Stream = false,
            Options = new RequestOptions
            {
                // Clasificar no es escribir: no se quiere variedad, se quiere la mejor
                // respuesta. Con la temperatura 0.8 que Ollama trae por defecto, el mismo
                // mensaje se clasifica distinto entre corridas —lo vimos: un mensaje acertaba
                // o fallaba según la tirada— y la accuracy medida deja de ser reproducible.
                Temperature = 0f,
                Seed = 1,
            },
        };

        var contenido = new StringBuilder();

        try
        {
            await foreach (var parte in _cliente.ChatAsync(pedido, cancellationToken))
            {
                contenido.Append(parte?.Message?.Content);

                if (contenido.Length > MaximoRespuesta)
                {
                    return null;
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout del HttpClient: HttpClient cancela la operación, no lanza una excepción
            // de timeout propia. Que el token del llamador NO esté cancelado es lo que
            // distingue el timeout de una cancelación real, que sí debe propagarse.
            return null;
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            // Ollama caído, conexión rechazada o respuesta ilegible: es falla de
            // infraestructura, y el caso de uso la trata distinto de un dato malo (FR-12).
            return null;
        }

        return contenido.ToString();
    }

    private static ResultadoClasificacion Interpretar(string contenido, IReadOnlyList<Categoria> categoriasActivas)
    {
        JsonDocument documento;

        try
        {
            documento = JsonDocument.Parse(contenido);
        }
        catch (JsonException)
        {
            return new ResultadoClasificacion.NoDisponible();
        }

        using (documento)
        {
            var raiz = documento.RootElement;

            if (raiz.ValueKind != JsonValueKind.Object)
            {
                return new ResultadoClasificacion.NoDisponible();
            }

            if (LeerMonto(raiz) is not { } monto || monto <= 0)
            {
                return new ResultadoClasificacion.SinMonto();
            }

            if (LeerTipo(raiz) is not { } tipo)
            {
                return new ResultadoClasificacion.TipoNoReconocido();
            }

            var categoria = ResolverCategoria(LeerTexto(raiz, "categoria"), categoriasActivas);

            return categoria is null
                // El modelo devolvió una categoría desconocida y "Otros" no está activa, así que
                // no hay dónde caer. Se trata como falla, no como error del mensaje: el mensaje
                // queda intacto y se reintenta cuando el seed esté completo.
                ? new ResultadoClasificacion.NoDisponible()
                : new ResultadoClasificacion.Clasificado(monto, tipo, categoria);
        }
    }

    private static decimal? LeerMonto(JsonElement raiz)
    {
        if (!raiz.TryGetProperty("monto", out var monto))
        {
            return null;
        }

        return monto.ValueKind switch
        {
            JsonValueKind.Number when monto.TryGetDecimal(out var valor) => valor,
            JsonValueKind.String when decimal.TryParse(
                monto.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) => valor,
            _ => null,
        };
    }

    /// <summary>
    /// Traduce el verbo que contesta el modelo al vocabulario del dominio.
    /// </summary>
    private static TipoMovimiento? LeerTipo(JsonElement raiz) => LeerTexto(raiz, "tipo") switch
    {
        var tipo when string.Equals(tipo, EsquemaClasificacion.Entro, StringComparison.OrdinalIgnoreCase)
            => TipoMovimiento.Ingreso,
        var tipo when string.Equals(tipo, EsquemaClasificacion.Salio, StringComparison.OrdinalIgnoreCase)
            => TipoMovimiento.Egreso,
        _ => null,
    };

    /// <summary>
    /// FR-09: una categoría fuera de las activas cae en <c>Otros</c>, no rompe la clasificación.
    /// </summary>
    private static Categoria? ResolverCategoria(string? titulo, IReadOnlyList<Categoria> categoriasActivas) =>
        categoriasActivas.FirstOrDefault(c => string.Equals(c.Titulo, titulo, StringComparison.OrdinalIgnoreCase))
        ?? categoriasActivas.FirstOrDefault(c => string.Equals(c.Titulo, Descarte, StringComparison.OrdinalIgnoreCase));

    private static string? LeerTexto(JsonElement raiz, string propiedad) =>
        raiz.TryGetProperty(propiedad, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;
}
