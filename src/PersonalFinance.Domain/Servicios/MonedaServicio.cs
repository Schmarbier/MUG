using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Servicios;

/// <summary>Alta, edición de cotización, eliminación/desactivación y reactivación de monedas (US6).</summary>
public class MonedaServicio(IMonedaRepositorio monedaRepositorio)
{
    public Task<IReadOnlyList<Moneda>> ListarAsync(CancellationToken ct = default) =>
        monedaRepositorio.ListarTodasAsync(ct);

    public async Task<Moneda> CrearAsync(string codigo, decimal tipoDeCambio, CancellationToken ct = default)
    {
        if (tipoDeCambio <= 0)
        {
            throw new InvalidOperationException("El tipo de cambio debe ser mayor a cero.");
        }

        if (await monedaRepositorio.ObtenerPorCodigoAsync(codigo, ct) is not null)
        {
            throw new InvalidOperationException($"Ya existe una moneda con el código '{codigo}'.");
        }

        var moneda = new Moneda { Codigo = codigo, EsBase = false, Activa = true, TipoDeCambio = tipoDeCambio };
        await monedaRepositorio.AgregarAsync(moneda, ct);
        await monedaRepositorio.GuardarCambiosAsync(ct);
        return moneda;
    }

    public async Task EditarCotizacionAsync(int id, decimal tipoDeCambio, CancellationToken ct = default)
    {
        if (tipoDeCambio <= 0)
        {
            throw new InvalidOperationException("El tipo de cambio debe ser mayor a cero.");
        }

        var moneda = await ObtenerAsync(id, ct);
        // Solo cambia la cotización vigente; los movimientos ya creados guardan su propio
        // TipoDeCambioHistorico y no se tocan acá (FR-035).
        moneda.TipoDeCambio = tipoDeCambio;
        await monedaRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var moneda = await ObtenerAsync(id, ct);
        if (moneda.EsBase)
        {
            throw new InvalidOperationException("La moneda base no puede eliminarse ni desactivarse.");
        }

        if (await monedaRepositorio.TieneMovimientosAsync(id, ct))
        {
            moneda.Activa = false;
            await monedaRepositorio.GuardarCambiosAsync(ct);
            return;
        }

        await monedaRepositorio.EliminarAsync(moneda, ct);
        await monedaRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task ReactivarAsync(int id, CancellationToken ct = default)
    {
        var moneda = await ObtenerAsync(id, ct);
        moneda.Activa = true;
        await monedaRepositorio.GuardarCambiosAsync(ct);
    }

    private async Task<Moneda> ObtenerAsync(int id, CancellationToken ct) =>
        await monedaRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new InvalidOperationException("La moneda no existe.");
}
