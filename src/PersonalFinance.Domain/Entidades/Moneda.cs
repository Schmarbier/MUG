namespace PersonalFinance.Domain.Entidades;

public class Moneda
{
    public int Id { get; set; }
    public required string Codigo { get; set; }
    public bool EsBase { get; set; }
    public bool Activa { get; set; }

    /// <summary>Nulo si y solo si es la moneda base (FR-032, FR-035).</summary>
    public decimal? TipoDeCambio { get; set; }
}
