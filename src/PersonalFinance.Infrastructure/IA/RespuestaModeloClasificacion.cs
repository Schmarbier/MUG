using System.Text.Json.Serialization;

namespace PersonalFinance.Infrastructure.IA;

/// <summary>Esquema JSON estricto exigido al modelo (contracts/clasificador.md).</summary>
internal sealed class RespuestaModeloClasificacion
{
    [JsonPropertyName("monto")]
    public decimal? Monto { get; set; }

    [JsonPropertyName("tipo")]
    public string? Tipo { get; set; }

    [JsonPropertyName("categoria")]
    public string? Categoria { get; set; }

    [JsonPropertyName("moneda")]
    public string? Moneda { get; set; }

    [JsonPropertyName("confianza")]
    public double? Confianza { get; set; }
}
