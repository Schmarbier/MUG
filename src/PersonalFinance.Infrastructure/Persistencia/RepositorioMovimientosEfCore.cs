using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Persistencia;

/// <summary>
/// Adaptador de <see cref="IRepositorioMovimientos"/> sobre EF Core. No confirma: la confirmación
/// es responsabilidad de <see cref="IUnitOfWork"/>, que es lo que hace atómico el par
/// movimiento + estado del mensaje.
/// </summary>
public sealed class RepositorioMovimientosEfCore : IRepositorioMovimientos
{
    private readonly PersonalFinanceDbContext _contexto;

    public RepositorioMovimientosEfCore(PersonalFinanceDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    public async Task AgregarAsync(Movimiento movimiento, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(movimiento);

        await _contexto.Movimientos.AddAsync(movimiento, cancellationToken);
    }
}
