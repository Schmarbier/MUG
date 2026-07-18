namespace PersonalFinance.Domain.Entidades;

public class Movimiento
{
    public int Id { get; set; }
    public int MensajeId { get; set; }
    public int CategoriaId { get; set; }
    public int MonedaId { get; set; }

    /// <summary>Exactamente 2 decimales, estrictamente mayor a cero (FR-038).</summary>
    public decimal Monto { get; set; }

    public TipoMovimiento Tipo { get; set; }

    /// <summary>Fecha local derivada de FechaRecepcionUtc del mensaje origen (R5).</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Nulo si y solo si la moneda del movimiento es la base (FR-035).</summary>
    public decimal? TipoDeCambioHistorico { get; set; }
}
