using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure;

public class ConsultaResumen : IConsultaResumen
{
    private readonly AppDbContext _db;

    public ConsultaResumen(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MovimientoResumen>> ObtenerMovimientosDelMesAsync(
        int anio, int mes, CancellationToken ct = default)
        => await _db.Movimientos
            .Where(m => m.Fecha.Year == anio && m.Fecha.Month == mes)
            .Select(m => new MovimientoResumen(
                m.Tipo, m.Categoria!.Titulo, m.MonedaCodigo, m.Monto, m.TipoCambioHistorico))
            .ToListAsync(ct);
}
