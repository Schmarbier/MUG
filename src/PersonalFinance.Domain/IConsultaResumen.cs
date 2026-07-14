namespace PersonalFinance.Domain;

public interface IConsultaResumen
{
    Task<IReadOnlyList<MovimientoResumen>> ObtenerMovimientosDelMesAsync(
        int anio, int mes, CancellationToken ct = default);
}
