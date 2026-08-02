using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Dobles;

/// <summary>
/// Doble de <see cref="IRepositorioMovimientos"/>. Igual que el adaptador real, no confirma:
/// eso es de la unidad de trabajo.
/// </summary>
internal sealed class RepositorioMovimientosEnMemoria : IRepositorioMovimientos
{
    public List<Movimiento> Agregados { get; } = [];

    public Task AgregarAsync(Movimiento movimiento, CancellationToken cancellationToken)
    {
        Agregados.Add(movimiento);

        return Task.CompletedTask;
    }
}
