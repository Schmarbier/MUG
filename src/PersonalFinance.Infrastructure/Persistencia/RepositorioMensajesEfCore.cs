using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Persistencia;

/// <summary>
/// Adaptador de <see cref="IRepositorioMensajes"/> sobre EF Core. No confirma: la confirmación
/// es responsabilidad de <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class RepositorioMensajesEfCore : IRepositorioMensajes
{
    private readonly PersonalFinanceDbContext _contexto;

    public RepositorioMensajesEfCore(PersonalFinanceDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <summary>Resuelve FR-04 con una consulta sobre el índice único de MessageId.</summary>
    public Task<bool> ExisteAsync(long messageId, CancellationToken cancellationToken) =>
        _contexto.Mensajes.AnyAsync(m => m.MessageId == messageId, cancellationToken);

    public async Task AgregarAsync(Mensaje mensaje, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mensaje);

        await _contexto.Mensajes.AddAsync(mensaje, cancellationToken);
    }

    public async Task<IReadOnlyList<Mensaje>> ObtenerPendientesAsync(CancellationToken cancellationToken) =>
        await _contexto.Mensajes
            .Where(m => !m.Procesado && !m.Error)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);
}
