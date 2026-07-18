using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Persistencia.Repositorios;

public class MonedaRepositorio(PersonalFinanceDbContext contexto) : IMonedaRepositorio
{
    public Task<Moneda?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Monedas.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<Moneda?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default) =>
        contexto.Monedas.FirstOrDefaultAsync(m => m.Codigo == codigo, ct);

    public async Task<IReadOnlyList<Moneda>> ListarActivasAsync(CancellationToken ct = default) =>
        await contexto.Monedas.Where(m => m.Activa).ToListAsync(ct);

    public async Task<IReadOnlyList<Moneda>> ListarTodasAsync(CancellationToken ct = default) =>
        await contexto.Monedas.ToListAsync(ct);

    public Task<Moneda> ObtenerBaseAsync(CancellationToken ct = default) =>
        contexto.Monedas.SingleAsync(m => m.EsBase, ct);

    public Task<bool> TieneMovimientosAsync(int monedaId, CancellationToken ct = default) =>
        contexto.Movimientos.AnyAsync(m => m.MonedaId == monedaId, ct);

    public Task AgregarAsync(Moneda moneda, CancellationToken ct = default)
    {
        contexto.Monedas.Add(moneda);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Moneda moneda, CancellationToken ct = default)
    {
        contexto.Monedas.Remove(moneda);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        contexto.SaveChangesAsync(ct);
}
