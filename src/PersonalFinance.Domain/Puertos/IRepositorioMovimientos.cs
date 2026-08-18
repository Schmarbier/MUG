using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Puertos;

/// <summary>
/// Puerto de persistencia de movimientos.
/// </summary>
public interface IRepositorioMovimientos
{
    Task AgregarAsync(Movimiento movimiento, CancellationToken cancellationToken);
}
