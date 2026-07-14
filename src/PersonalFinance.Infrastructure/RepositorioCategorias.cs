using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure;

public class RepositorioCategorias : IRepositorioCategorias
{
    private readonly AppDbContext _db;

    public RepositorioCategorias(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(CancellationToken ct = default)
        => await _db.Categorias.Where(c => c.Activa).ToListAsync(ct);
}
