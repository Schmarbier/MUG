using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Persistencia.Repositorios;

public class MovimientoRepositorio(PersonalFinanceDbContext contexto) : IMovimientoRepositorio
{
    public Task<Movimiento?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Movimientos.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<Movimiento>> ListarPorMesAsync(int anio, int mes, CancellationToken ct = default)
    {
        var desde = new DateOnly(anio, mes, 1);
        var hasta = desde.AddMonths(1);
        return await contexto.Movimientos
            .Where(m => m.Fecha >= desde && m.Fecha < hasta)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Movimiento>> ListarPorMonedaYFechaAsync(int monedaId, DateOnly fecha, CancellationToken ct = default) =>
        await contexto.Movimientos
            .Where(m => m.MonedaId == monedaId && m.Fecha == fecha)
            .ToListAsync(ct);

    public Task<bool> ExistePorCategoriaAsync(int categoriaId, CancellationToken ct = default) =>
        contexto.Movimientos.AnyAsync(m => m.CategoriaId == categoriaId, ct);

    public Task<bool> ExistePorMonedaAsync(int monedaId, CancellationToken ct = default) =>
        contexto.Movimientos.AnyAsync(m => m.MonedaId == monedaId, ct);

    public Task<bool> ExistePorMensajeAsync(int mensajeId, CancellationToken ct = default) =>
        contexto.Movimientos.AnyAsync(m => m.MensajeId == mensajeId, ct);

    public Task AgregarAsync(Movimiento movimiento, CancellationToken ct = default)
    {
        contexto.Movimientos.Add(movimiento);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Movimiento movimiento, CancellationToken ct = default)
    {
        contexto.Movimientos.Remove(movimiento);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        contexto.SaveChangesAsync(ct);
}
