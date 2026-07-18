using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Falsos;

public sealed class RepositorioMonedaFalso : IMonedaRepositorio
{
    public List<Moneda> Monedas { get; } = [];
    public HashSet<int> TieneMovimientosPorMoneda { get; } = [];

    public Task<Moneda?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Monedas.FirstOrDefault(m => m.Id == id));

    public Task<Moneda?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default) =>
        Task.FromResult(Monedas.FirstOrDefault(m => m.Codigo == codigo));

    public Task<IReadOnlyList<Moneda>> ListarActivasAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Moneda>>(Monedas.Where(m => m.Activa).ToList());

    public Task<IReadOnlyList<Moneda>> ListarTodasAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Moneda>>([.. Monedas]);

    public Task<Moneda> ObtenerBaseAsync(CancellationToken ct = default) =>
        Task.FromResult(Monedas.Single(m => m.EsBase));

    public Task<bool> TieneMovimientosAsync(int monedaId, CancellationToken ct = default) =>
        Task.FromResult(TieneMovimientosPorMoneda.Contains(monedaId));

    public Task AgregarAsync(Moneda moneda, CancellationToken ct = default)
    {
        moneda.Id = Monedas.Count == 0 ? 1 : Monedas.Max(m => m.Id) + 1;
        Monedas.Add(moneda);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Moneda moneda, CancellationToken ct = default)
    {
        Monedas.Remove(moneda);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => Task.CompletedTask;
}
