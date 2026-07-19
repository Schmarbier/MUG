using System.Text.Json;
using OllamaSharp;
using OllamaSharp.Models;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.IA;

/// <summary>
/// Único punto de contacto con el modelo (Principio II): prompt, parseo del JSON,
/// timeout y umbral de confianza viven acá. El dominio nunca ve nada de esto.
/// </summary>
public class OllamaClasificadorAdapter(
    IOllamaApiClient cliente,
    string modelo,
    TimeSpan timeout,
    double umbralConfianza = 0.6) : IClasificadorDeMensajes
{
    private static readonly JsonSerializerOptions JsonOpciones = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Salida estructurada de Ollama (constrained decoding): a diferencia de Format="json" —que
    /// solo garantiza sintaxis JSON válida—, este esquema obliga a que "confianza" y el resto de
    /// las claves requeridas estén siempre presentes. Sin esto, el modelo omite "confianza" en la
    /// mayoría de las respuestas y todo termina en Falla(SinConfianza) (medido: SC-001 caía a ~13%).
    /// </summary>
    private static readonly JsonElement EsquemaRespuesta = JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "monto": { "type": "string" },
            "tipo": { "type": "string", "enum": ["ingreso", "egreso"] },
            "categoria": { "type": "string" },
            "moneda": { "type": "string" },
            "confianza": { "type": "number" }
          },
          "required": ["monto", "tipo", "categoria", "confianza"]
        }
        """).RootElement;

    public async Task<ResultadoClasificacion> ClasificarAsync(
        string texto,
        IReadOnlyList<CategoriaActiva> categoriasActivas,
        IReadOnlyList<MonedaActiva> monedasActivas,
        CancellationToken ct = default)
    {
        string respuestaCruda;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            var solicitud = new GenerateRequest
            {
                Model = modelo,
                Prompt = ArmarPrompt(texto, categoriasActivas, monedasActivas),
                Stream = false,
                Format = EsquemaRespuesta,
                Options = new RequestOptions { Temperature = 0.1f },
                // Evita que Ollama descargue el modelo entre mensajes: una recarga en frío mide
                // ~6.6s en hardware de referencia, más que el timeout por llamada (SC-002).
                KeepAlive = "30m"
            };

            GenerateResponseStream? ultimaRespuesta = null;
            await foreach (var fragmento in cliente.GenerateAsync(solicitud, cts.Token))
            {
                ultimaRespuesta = fragmento;
            }

            if (ultimaRespuesta?.Response is null)
            {
                return Falla(MotivoFalla.ClasificadorNoDisponible);
            }

            respuestaCruda = ultimaRespuesta.Response;
        }
        catch (OperationCanceledException)
        {
            return Falla(MotivoFalla.ClasificadorNoDisponible);
        }
        catch (HttpRequestException)
        {
            return Falla(MotivoFalla.ClasificadorNoDisponible);
        }

        return Interpretar(respuestaCruda, categoriasActivas, monedasActivas, umbralConfianza);
    }

    private static ResultadoClasificacion Interpretar(
        string json,
        IReadOnlyList<CategoriaActiva> categoriasActivas,
        IReadOnlyList<MonedaActiva> monedasActivas,
        double umbralConfianza)
    {
        RespuestaModeloClasificacion? respuesta;
        try
        {
            respuesta = JsonSerializer.Deserialize<RespuestaModeloClasificacion>(json, JsonOpciones);
        }
        catch (JsonException)
        {
            return Falla(MotivoFalla.SinConfianza);
        }

        if (respuesta is null)
        {
            return Falla(MotivoFalla.SinConfianza);
        }

        if (!MontoArgentinoParser.TryParsear(respuesta.Monto, out var monto) || monto <= 0)
        {
            return Falla(MotivoFalla.SinMonto);
        }

        if (string.IsNullOrWhiteSpace(respuesta.Categoria))
        {
            return Falla(MotivoFalla.SinDescripcion);
        }

        if (respuesta.Confianza is not double confianza || confianza < 0 || confianza > 1 || confianza < umbralConfianza)
        {
            return Falla(MotivoFalla.SinConfianza);
        }

        if (!TryMapearTipo(respuesta.Tipo, out var tipo))
        {
            return Falla(MotivoFalla.SinConfianza);
        }

        var categoria = categoriasActivas.FirstOrDefault(c =>
            string.Equals(c.Titulo, respuesta.Categoria, StringComparison.OrdinalIgnoreCase));
        if (categoria is null)
        {
            return Falla(MotivoFalla.SinConfianza);
        }

        string? codigoMoneda = null;
        if (!string.IsNullOrWhiteSpace(respuesta.Moneda))
        {
            var moneda = monedasActivas.FirstOrDefault(m =>
                string.Equals(m.Codigo, respuesta.Moneda, StringComparison.OrdinalIgnoreCase));
            if (moneda is null)
            {
                return Falla(MotivoFalla.MonedaNoSoportada);
            }

            codigoMoneda = moneda.Codigo;
        }

        return new ResultadoClasificacion.Exitosa(new Clasificacion(monto, tipo, categoria.Titulo, codigoMoneda));
    }

    private static bool TryMapearTipo(string? tipo, out TipoMovimiento resultado)
    {
        switch (tipo?.Trim().ToLowerInvariant())
        {
            case "ingreso":
                resultado = TipoMovimiento.Ingreso;
                return true;
            case "egreso":
                resultado = TipoMovimiento.Egreso;
                return true;
            default:
                resultado = default;
                return false;
        }
    }

    private static ResultadoClasificacion.Fallida Falla(MotivoFalla motivo) => new(new Falla(motivo));

    private static string ArmarPrompt(
        string texto,
        IReadOnlyList<CategoriaActiva> categoriasActivas,
        IReadOnlyList<MonedaActiva> monedasActivas)
    {
        var categorias = string.Join("\n", categoriasActivas.Select(c => $"- {c.Titulo}: {c.Descripcion}"));
        var monedas = string.Join(", ", monedasActivas.Select(m => m.Codigo));

        return $"""
            Clasificá el siguiente mensaje de gasto o ingreso personal.

            Reglas:
            - "monto" es el número tal cual aparece escrito en el mensaje, en texto, SIN hacer
              ninguna cuenta ni conversión vos mismo (por ejemplo, si el mensaje dice "10,22"
              escribí exactamente "10,22", no "1022" ni "10.22").
            - "tipo" es "egreso" salvo que el mensaje indique explícitamente un ingreso (cobro de
              sueldo, aguinaldo, intereses, que le pagaron, que le devolvieron dinero, etc.).
            - "categoria" MUST ser exactamente uno de los títulos de la lista de abajo, tal cual
              está escrito.
            - "confianza" es un número entre 0 y 1 que indica qué tan seguro estás de la categoría
              elegida; siempre incluila.
            - Si el mensaje no especifica ninguna moneda, no incluyas la clave "moneda".
            - Si el mensaje menciona una moneda aunque NO esté en la lista de "Monedas disponibles"
              (por ejemplo dólares, euros), igual incluila en "moneda" con su código de 3 letras
              (USD, EUR, etc.) — no la descartes ni asumas la moneda por defecto.

            Categorías disponibles:
            {categorias}

            Monedas disponibles: {monedas}

            Mensaje: "{texto}"
            """;
    }
}
