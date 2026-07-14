using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure;

// Adaptador EF del puerto IRepositorioMensajes.
public class RepositorioMensajes : IRepositorioMensajes
{
    private readonly AppDbContext _db;

    public RepositorioMensajes(AppDbContext db) => _db = db;

    public Task<bool> ExisteAsync(long messageId, CancellationToken ct = default)
        => _db.Mensajes.AnyAsync(m => m.MessageId == messageId, ct);

    public async Task AgregarAsync(Mensaje mensaje, CancellationToken ct = default)
    {
        _db.Mensajes.Add(mensaje);
        await _db.SaveChangesAsync(ct);
    }
}
