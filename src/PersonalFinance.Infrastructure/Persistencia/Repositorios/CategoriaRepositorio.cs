using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Persistencia.Repositorios;

public class CategoriaRepositorio(PersonalFinanceDbContext contexto) : ICategoriaRepositorio
{
    public Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Categoria?> ObtenerPorTituloAsync(string titulo, CancellationToken ct = default) =>
        contexto.Categorias.FirstOrDefaultAsync(c => c.Titulo == titulo, ct);

    public async Task<IReadOnlyList<Categoria>> ListarActivasAsync(CancellationToken ct = default) =>
        await contexto.Categorias.Where(c => c.Activa).ToListAsync(ct);

    public async Task<IReadOnlyList<Categoria>> ListarTodasAsync(CancellationToken ct = default) =>
        await contexto.Categorias.ToListAsync(ct);

    public Task<bool> TieneMovimientosAsync(int categoriaId, CancellationToken ct = default) =>
        contexto.Movimientos.AnyAsync(m => m.CategoriaId == categoriaId, ct);

    public Task AgregarAsync(Categoria categoria, CancellationToken ct = default)
    {
        contexto.Categorias.Add(categoria);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Categoria categoria, CancellationToken ct = default)
    {
        contexto.Categorias.Remove(categoria);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        contexto.SaveChangesAsync(ct);
}
