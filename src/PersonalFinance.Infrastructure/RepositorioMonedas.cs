using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure;

public class RepositorioMonedas : IRepositorioMonedas
{
    private readonly AppDbContext _db;

    public RepositorioMonedas(AppDbContext db) => _db = db;

    public Task<Moneda?> ObtenerAsync(string codigo, CancellationToken ct = default)
        => _db.Monedas.FirstOrDefaultAsync(m => m.Codigo == codigo, ct);
}
