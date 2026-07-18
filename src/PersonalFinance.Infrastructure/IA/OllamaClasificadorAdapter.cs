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
                Format = "json",
                Options = new RequestOptions { Temperature = 0.1f }
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

        if (respuesta.Monto is not decimal monto || monto <= 0 || decimal.Round(monto, 2) != monto)
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
            Clasificá el siguiente mensaje de gasto o ingreso personal en JSON estricto con
            las claves monto, tipo ("ingreso" o "egreso"), categoria, moneda y confianza (0 a 1).
            Si el mensaje no especifica moneda, omití la clave moneda.

            Categorías disponibles:
            {categorias}

            Monedas disponibles: {monedas}

            Mensaje: "{texto}"
            """;
    }
}
