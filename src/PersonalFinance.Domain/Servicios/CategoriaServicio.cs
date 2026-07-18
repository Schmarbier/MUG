using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Servicios;

/// <summary>Alta, edición, eliminación/desactivación y reactivación de categorías (US3).</summary>
public class CategoriaServicio(ICategoriaRepositorio categoriaRepositorio)
{
    public Task<IReadOnlyList<Categoria>> ListarAsync(CancellationToken ct = default) =>
        categoriaRepositorio.ListarTodasAsync(ct);

    public async Task<Categoria> CrearAsync(string titulo, string descripcion, CancellationToken ct = default)
    {
        if (await categoriaRepositorio.ObtenerPorTituloAsync(titulo, ct) is not null)
        {
            throw new InvalidOperationException($"Ya existe una categoría con el título '{titulo}'.");
        }

        var categoria = new Categoria { Titulo = titulo, Descripcion = descripcion, Activa = true };
        await categoriaRepositorio.AgregarAsync(categoria, ct);
        await categoriaRepositorio.GuardarCambiosAsync(ct);
        return categoria;
    }

    public async Task EditarAsync(int id, string titulo, string descripcion, CancellationToken ct = default)
    {
        var categoria = await categoriaRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new InvalidOperationException("La categoría no existe.");

        var existente = await categoriaRepositorio.ObtenerPorTituloAsync(titulo, ct);
        if (existente is not null && existente.Id != id)
        {
            throw new InvalidOperationException($"Ya existe una categoría con el título '{titulo}'.");
        }

        // Activa no se toca: editar no altera el estado (FR-026).
        categoria.Titulo = titulo;
        categoria.Descripcion = descripcion;
        await categoriaRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var categoria = await categoriaRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new InvalidOperationException("La categoría no existe.");

        if (await categoriaRepositorio.TieneMovimientosAsync(id, ct))
        {
            categoria.Activa = false;
            await categoriaRepositorio.GuardarCambiosAsync(ct);
            return;
        }

        await categoriaRepositorio.EliminarAsync(categoria, ct);
        await categoriaRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task ReactivarAsync(int id, CancellationToken ct = default)
    {
        var categoria = await categoriaRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new InvalidOperationException("La categoría no existe.");

        categoria.Activa = true;
        await categoriaRepositorio.GuardarCambiosAsync(ct);
    }
}
