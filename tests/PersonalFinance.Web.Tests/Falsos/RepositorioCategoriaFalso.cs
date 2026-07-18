using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Web.Tests.Falsos;

/// <summary>Doble en memoria; sin Moq ni infraestructura, per R9 (Principio I sin mocking framework).</summary>
public sealed class RepositorioCategoriaFalso : ICategoriaRepositorio
{
    public List<Categoria> Categorias { get; } = [];

    public Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Categorias.FirstOrDefault(c => c.Id == id));

    public Task<Categoria?> ObtenerPorTituloAsync(string titulo, CancellationToken ct = default) =>
        Task.FromResult(Categorias.FirstOrDefault(c => c.Titulo == titulo));

    public Task<IReadOnlyList<Categoria>> ListarActivasAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Categoria>>(Categorias.Where(c => c.Activa).ToList());

    public Task<IReadOnlyList<Categoria>> ListarTodasAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Categoria>>([.. Categorias]);

    public Task<bool> TieneMovimientosAsync(int categoriaId, CancellationToken ct = default) =>
        Task.FromResult(TieneMovimientosPorCategoria.Contains(categoriaId));

    public Task AgregarAsync(Categoria categoria, CancellationToken ct = default)
    {
        categoria.Id = Categorias.Count == 0 ? 1 : Categorias.Max(c => c.Id) + 1;
        Categorias.Add(categoria);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Categoria categoria, CancellationToken ct = default)
    {
        Categorias.Remove(categoria);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>IDs de categoría que el test quiere simular como "con movimientos asociados".</summary>
    public HashSet<int> TieneMovimientosPorCategoria { get; } = [];
}
