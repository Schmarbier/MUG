namespace PersonalFinance.Domain;

// Codigo es la clave (ej. "ARS", "USD"). La moneda base (ARS) usa TipoCambio = 1
// y EsBase = true; RF-24: los montos ya se expresan en ella, no requiere cotización.
public class Moneda
{
    public string Codigo { get; set; } = default!;
    public decimal TipoCambio { get; set; }
    public bool EsBase { get; set; }
}
