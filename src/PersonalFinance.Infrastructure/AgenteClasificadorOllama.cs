using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure;

// Adaptador del puerto IAgenteClasificador contra un modelo local vía Ollama. No se unit-testea
// (depende de un LLM externo); la lógica que valida su salida sí (ServicioClasificacion).
public class AgenteClasificadorOllama : IAgenteClasificador
{
    private readonly IOllamaApiClient _cliente;
    private readonly ILogger<AgenteClasificadorOllama> _logger;

    public AgenteClasificadorOllama(IOllamaApiClient cliente, ILogger<AgenteClasificadorOllama> logger)
    {
        _cliente = cliente;
        _logger = logger;
    }

    public async Task<ExtraccionMovimiento> ExtraerAsync(
        string texto, IReadOnlyList<Categoria> categoriasActivas, CancellationToken ct = default)
    {
        var listaCategorias = string.Join(", ", categoriasActivas.Select(c => c.Titulo));
        var request = new GenerateRequest
        {
            Prompt = texto,
            System = SistemaPrompt(listaCategorias),
            Format = "json", // modo JSON de Ollama: fuerza salida parseable
            Stream = false,
        };

        var sb = new StringBuilder();
        await foreach (var chunk in _cliente.GenerateAsync(request, ct))
            if (chunk is not null)
                sb.Append(chunk.Response);

        return Parsear(sb.ToString());
    }

    private ExtraccionMovimiento Parsear(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<RespuestaLlm>(json, OpcionesJson);
            if (dto is null)
                return VacioParaError();

            var tipo = string.Equals(dto.Tipo, "ingreso", StringComparison.OrdinalIgnoreCase)
                ? TipoMovimiento.Ingreso
                : TipoMovimiento.Egreso;

            return new ExtraccionMovimiento(
                dto.Monto,
                dto.Descripcion,
                tipo,
                string.IsNullOrWhiteSpace(dto.Categoria) ? null : dto.Categoria.Trim(),
                string.IsNullOrWhiteSpace(dto.Moneda) ? null : dto.Moneda.Trim().ToUpperInvariant());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[procesador] respuesta del modelo no es JSON válido: {Json}", json);
            return VacioParaError();
        }
    }

    // Extracción "vacía": el ServicioClasificacion la traduce a error "no contiene monto".
    private static ExtraccionMovimiento VacioParaError()
        => new(null, null, TipoMovimiento.Egreso, null, null);

    private static string SistemaPrompt(string categorias) => $$"""
        Sos un clasificador de finanzas personales. Devolvés SOLO un JSON con esta forma exacta,
        sin ningún texto adicional:
        {"monto": number|null, "descripcion": string|null, "tipo": "ingreso"|"egreso", "categoria": string|null, "moneda": string|null}

        Reglas:
        - monto: el número del movimiento, sin símbolos ni separadores de miles (ej. "$10.000" => 10000). Si no hay, null.
        - descripcion: breve, qué es el movimiento. Si no hay, null.
        - tipo: "ingreso" si entra dinero, "egreso" si sale.
        - categoria: elegí exactamente una de esta lista: {{categorias}}.
        - moneda: código ISO (USD, EUR, ...) solo si el mensaje menciona una moneda. Si no menciona, null.
        """;

    private static readonly JsonSerializerOptions OpcionesJson = new() { PropertyNameCaseInsensitive = true };

    private sealed record RespuestaLlm(
        decimal? Monto, string? Descripcion, string? Tipo, string? Categoria, string? Moneda);
}
