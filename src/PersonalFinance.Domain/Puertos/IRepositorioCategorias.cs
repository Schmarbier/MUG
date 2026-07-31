using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Puertos;

/// <summary>
/// Puerto de persistencia de categorías.
/// </summary>
public interface IRepositorioCategorias
{
    /// <summary>Sólo las activas participan de la clasificación (FR-08).</summary>
    Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(CancellationToken cancellationToken);
}
