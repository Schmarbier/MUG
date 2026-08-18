using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Dobles;

/// <summary>
/// Doble de <see cref="IRepositorioCategorias"/>.
/// </summary>
internal sealed class RepositorioCategoriasFalso : IRepositorioCategorias
{
    private readonly IReadOnlyList<Categoria> _activas;

    public RepositorioCategoriasFalso(params Categoria[] activas) => _activas = activas;

    public Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_activas);
}
