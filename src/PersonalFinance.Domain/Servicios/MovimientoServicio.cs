using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Servicios;

/// <summary>Corrección manual de un movimiento existente (US5).</summary>
public class MovimientoServicio(IMovimientoRepositorio movimientoRepositorio, IMonedaRepositorio monedaRepositorio)
{
    public async Task EditarCategoriaAsync(int movimientoId, int categoriaId, CancellationToken ct = default)
    {
        var movimiento = await ObtenerAsync(movimientoId, ct);
        movimiento.CategoriaId = categoriaId;
        await movimientoRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task EditarMontoAsync(int movimientoId, decimal monto, CancellationToken ct = default)
    {
        if (monto <= 0)
        {
            throw new InvalidOperationException("El monto debe ser mayor a cero.");
        }

        var movimiento = await ObtenerAsync(movimientoId, ct);
        movimiento.Monto = monto;
        await movimientoRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task EditarMonedaAsync(int movimientoId, int monedaId, CancellationToken ct = default)
    {
        var movimiento = await ObtenerAsync(movimientoId, ct);
        var moneda = await monedaRepositorio.ObtenerPorIdAsync(monedaId, ct)
            ?? throw new InvalidOperationException("La moneda no existe.");

        movimiento.MonedaId = monedaId;
        // Se registra el tipo de cambio vigente al momento de la edición (FR-021).
        movimiento.TipoDeCambioHistorico = moneda.EsBase ? null : moneda.TipoDeCambio;
        await movimientoRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task EditarTipoAsync(int movimientoId, TipoMovimiento tipo, CancellationToken ct = default)
    {
        var movimiento = await ObtenerAsync(movimientoId, ct);
        // Cambia de bloque en el resumen; no toca monto, moneda ni tipo de cambio histórico (FR-018a).
        movimiento.Tipo = tipo;
        await movimientoRepositorio.GuardarCambiosAsync(ct);
    }

    private async Task<Movimiento> ObtenerAsync(int id, CancellationToken ct) =>
        await movimientoRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new InvalidOperationException("El movimiento no existe.");
}
