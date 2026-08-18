namespace PersonalFinance.Domain.Entidades;

/// <summary>
/// El registro estructurado que sale de clasificar un <see cref="Mensaje"/>.
/// Un Mensaje produce como máximo un Movimiento.
/// </summary>
public class Movimiento
{
    private Movimiento(long mensajeId, int categoriaId, decimal monto, TipoMovimiento tipo, DateTime fechaCreacion)
    {
        MensajeId = mensajeId;
        CategoriaId = categoriaId;
        Monto = monto;
        Tipo = tipo;
        FechaCreacion = fechaCreacion;
    }

    public long Id { get; private set; }

    public long MensajeId { get; private set; }

    public int CategoriaId { get; private set; }

    public decimal Monto { get; private set; }

    public TipoMovimiento Tipo { get; private set; }

    public DateTime FechaCreacion { get; private set; }

    /// <summary>
    /// Crea un movimiento validando sus invariantes: monto positivo y tipo dentro del enum.
    /// </summary>
    public static Movimiento Crear(
        long mensajeId,
        int categoriaId,
        decimal monto,
        TipoMovimiento tipo,
        DateTime fechaCreacion)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(monto, 0m, nameof(monto));

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipo), tipo, "El tipo del movimiento debe ser ingreso o egreso.");
        }

        return new Movimiento(mensajeId, categoriaId, monto, tipo, fechaCreacion);
    }
}
