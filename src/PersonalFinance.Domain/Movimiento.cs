namespace PersonalFinance.Domain;

public class Movimiento
{
    public int Id { get; set; }

    public int MensajeId { get; set; }
    public Mensaje? Mensaje { get; set; }

    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public TipoMovimiento Tipo { get; set; }
    public decimal Monto { get; set; }

    public string MonedaCodigo { get; set; } = "ARS";
    public Moneda? Moneda { get; set; }

    // Cotización congelada al crear el movimiento (RF-27). Null cuando es moneda base.
    public decimal? TipoCambioHistorico { get; set; }

    public DateTime Fecha { get; set; }
}
