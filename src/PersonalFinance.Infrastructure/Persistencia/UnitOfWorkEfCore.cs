using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Persistencia;

/// <summary>
/// Adaptador de <see cref="IUnitOfWork"/>. Confirma todos los cambios pendientes del mismo
/// <see cref="PersonalFinanceDbContext"/> en una sola operación: o se guardan todos, o ninguno.
/// Los tres repositorios reciben ese mismo contexto por DI dentro del scope de la corrida.
/// </summary>
public sealed class UnitOfWorkEfCore : IUnitOfWork
{
    private readonly PersonalFinanceDbContext _contexto;

    public UnitOfWorkEfCore(PersonalFinanceDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    public async Task ConfirmarAsync(CancellationToken cancellationToken) =>
        await _contexto.SaveChangesAsync(cancellationToken);
}
