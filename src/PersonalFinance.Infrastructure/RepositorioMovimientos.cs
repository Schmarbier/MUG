using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure;

public class RepositorioMovimientos : IRepositorioMovimientos
{
    private readonly AppDbContext _db;

    public RepositorioMovimientos(AppDbContext db) => _db = db;

    // Sin SaveChanges: el cierre lo hace RepositorioMensajes.GuardarCambiosAsync,
    // así el movimiento y el estado del mensaje se persisten en la misma transacción.
    public Task AgregarAsync(Movimiento movimiento, CancellationToken ct = default)
    {
        _db.Movimientos.Add(movimiento);
        return Task.CompletedTask;
    }
}
