using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Persistencia.Repositorios;

public class MensajeRepositorio(PersonalFinanceDbContext contexto) : IMensajeRepositorio
{
    public Task<bool> ExisteConIdentificadorCanalAsync(long identificadorCanal, CancellationToken ct = default) =>
        contexto.Mensajes.AnyAsync(m => m.IdentificadorCanal == identificadorCanal, ct);

    public Task<Mensaje?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Mensajes.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<Mensaje>> ListarPendientesAsync(CancellationToken ct = default) =>
        await contexto.Mensajes.Where(m => !m.Procesado && !m.TieneError).ToListAsync(ct);

    public async Task<IReadOnlyList<Mensaje>> ListarConErrorAsync(CancellationToken ct = default) =>
        await contexto.Mensajes.Where(m => m.TieneError).ToListAsync(ct);

    public Task AgregarAsync(Mensaje mensaje, CancellationToken ct = default)
    {
        contexto.Mensajes.Add(mensaje);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        contexto.SaveChangesAsync(ct);
}
