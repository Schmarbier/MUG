namespace PersonalFinance.Bot.Domain;

/// <summary>
/// Movimiento creado a partir de un mensaje (RF-04): un monto, un tipo (ingreso/egreso)
/// y una categoría asignada por el clasificador (RF-05).
/// </summary>
public class Movimiento
{
    public int Id { get; set; }

    public decimal Monto { get; set; }

    public TipoMovimiento Tipo { get; set; }

    public DateTimeOffset Fecha { get; set; }

    /// <summary>Mensaje del que se originó este movimiento.</summary>
    public int MensajeId { get; set; }
    public Mensaje? Mensaje { get; set; }

    /// <summary>Categoría asignada (RF-05, editable por RF-12).</summary>
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
}
