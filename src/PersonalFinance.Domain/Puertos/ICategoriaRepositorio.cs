using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Puertos;

public interface ICategoriaRepositorio
{
    Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Categoria?> ObtenerPorTituloAsync(string titulo, CancellationToken ct = default);
    Task<IReadOnlyList<Categoria>> ListarActivasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Categoria>> ListarTodasAsync(CancellationToken ct = default);
    Task<bool> TieneMovimientosAsync(int categoriaId, CancellationToken ct = default);
    Task AgregarAsync(Categoria categoria, CancellationToken ct = default);
    Task EliminarAsync(Categoria categoria, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
