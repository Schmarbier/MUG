using System.Text.Json.Serialization;

namespace PersonalFinance.Infrastructure.IA;

/// <summary>Esquema JSON estricto exigido al modelo (contracts/clasificador.md).</summary>
internal sealed class RespuestaModeloClasificacion
{
    /// <summary>
    /// Texto literal del monto tal como aparece en el mensaje (p. ej. "10,22" o "2.000"), no un
    /// número ya calculado: la conversión decimal↔texto se hace en MontoArgentinoParser, nunca
    /// confiando en la aritmética del modelo (medido: convertía "10,22" en 1022).
    /// </summary>
    [JsonPropertyName("monto")]
    public string? Monto { get; set; }

    [JsonPropertyName("tipo")]
    public string? Tipo { get; set; }

    [JsonPropertyName("categoria")]
    public string? Categoria { get; set; }

    [JsonPropertyName("moneda")]
    public string? Moneda { get; set; }

    [JsonPropertyName("confianza")]
    public double? Confianza { get; set; }
}
