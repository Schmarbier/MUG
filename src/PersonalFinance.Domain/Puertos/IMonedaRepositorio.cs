using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Puertos;

public interface IMonedaRepositorio
{
    Task<Moneda?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Moneda?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default);
    Task<IReadOnlyList<Moneda>> ListarActivasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Moneda>> ListarTodasAsync(CancellationToken ct = default);
    Task<Moneda> ObtenerBaseAsync(CancellationToken ct = default);
    Task<bool> TieneMovimientosAsync(int monedaId, CancellationToken ct = default);
    Task AgregarAsync(Moneda moneda, CancellationToken ct = default);
    Task EliminarAsync(Moneda moneda, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
