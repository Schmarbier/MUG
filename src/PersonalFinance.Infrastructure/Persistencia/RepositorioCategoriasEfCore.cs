using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Persistencia;

/// <summary>
/// Adaptador de <see cref="IRepositorioCategorias"/> sobre EF Core.
/// </summary>
public sealed class RepositorioCategoriasEfCore : IRepositorioCategorias
{
    private readonly PersonalFinanceDbContext _contexto;

    public RepositorioCategoriasEfCore(PersonalFinanceDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <summary>Sólo las activas participan de la clasificación (FR-08).</summary>
    public async Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(CancellationToken cancellationToken) =>
        await _contexto.Categorias
            .Where(c => c.Activa)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);
}
