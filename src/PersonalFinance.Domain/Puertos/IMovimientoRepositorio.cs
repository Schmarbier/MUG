using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Puertos;

public interface IMovimientoRepositorio
{
    Task<Movimiento?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Movimiento>> ListarPorMesAsync(int anio, int mes, CancellationToken ct = default);
    Task<IReadOnlyList<Movimiento>> ListarPorMonedaYFechaAsync(int monedaId, DateOnly fecha, CancellationToken ct = default);
    Task<bool> ExistePorCategoriaAsync(int categoriaId, CancellationToken ct = default);
    Task<bool> ExistePorMonedaAsync(int monedaId, CancellationToken ct = default);
    Task<bool> ExistePorMensajeAsync(int mensajeId, CancellationToken ct = default);
    Task AgregarAsync(Movimiento movimiento, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
