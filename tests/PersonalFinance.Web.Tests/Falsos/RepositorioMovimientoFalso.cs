using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Web.Tests.Falsos;

public sealed class RepositorioMovimientoFalso : IMovimientoRepositorio
{
    public List<Movimiento> Movimientos { get; } = [];

    public Task<Movimiento?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Movimientos.FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<Movimiento>> ListarPorMesAsync(int anio, int mes, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Movimiento>>(
            Movimientos.Where(m => m.Fecha.Year == anio && m.Fecha.Month == mes).ToList());

    public Task<IReadOnlyList<Movimiento>> ListarPorMonedaYFechaAsync(int monedaId, DateOnly fecha, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Movimiento>>(
            Movimientos.Where(m => m.MonedaId == monedaId && m.Fecha == fecha).ToList());

    public Task<bool> ExistePorCategoriaAsync(int categoriaId, CancellationToken ct = default) =>
        Task.FromResult(Movimientos.Any(m => m.CategoriaId == categoriaId));

    public Task<bool> ExistePorMonedaAsync(int monedaId, CancellationToken ct = default) =>
        Task.FromResult(Movimientos.Any(m => m.MonedaId == monedaId));

    public Task<bool> ExistePorMensajeAsync(int mensajeId, CancellationToken ct = default) =>
        Task.FromResult(Movimientos.Any(m => m.MensajeId == mensajeId));

    public Task AgregarAsync(Movimiento movimiento, CancellationToken ct = default)
    {
        movimiento.Id = Movimientos.Count == 0 ? 1 : Movimientos.Max(m => m.Id) + 1;
        Movimientos.Add(movimiento);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Movimiento movimiento, CancellationToken ct = default)
    {
        Movimientos.Remove(movimiento);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => Task.CompletedTask;
}
