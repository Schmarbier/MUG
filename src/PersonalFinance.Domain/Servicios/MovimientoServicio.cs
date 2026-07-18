using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Servicios;

/// <summary>Corrección manual de un movimiento existente (US5).</summary>
public class MovimientoServicio(
    IMovimientoRepositorio movimientoRepositorio,
    IMonedaRepositorio monedaRepositorio,
    ICategoriaRepositorio categoriaRepositorio)
{
    public async Task EditarCategoriaAsync(int movimientoId, int categoriaId, CancellationToken ct = default)
    {
        var movimiento = await ObtenerAsync(movimientoId, ct);
        var categoria = await categoriaRepositorio.ObtenerPorIdAsync(categoriaId, ct);
        // Una categoría inexistente o desactivada bloquea toda asignación nueva, tanto de la
        // clasificación automática (FR-031) como de la corrección manual (FR-018).
        if (categoria is null || !categoria.Activa)
        {
            throw new InvalidOperationException("La categoría no existe o está desactivada.");
        }

        movimiento.CategoriaId = categoriaId;
        await movimientoRepositorio.GuardarCambiosAsync(ct);
    }

    /// <summary>Reasigna el movimiento al mes de la nueva fecha, sin alterar sus demás campos (FR-020a).</summary>
    public async Task EditarFechaAsync(int movimientoId, DateOnly fecha, CancellationToken ct = default)
    {
        var movimiento = await ObtenerAsync(movimientoId, ct);
        movimiento.Fecha = fecha;
        await movimientoRepositorio.GuardarCambiosAsync(ct);
    }

    /// <summary>Eliminación definitiva; no afecta al Mensaje de origen ni a otros movimientos (FR-023a).</summary>
    public async Task EliminarAsync(int movimientoId, CancellationToken ct = default)
    {
        var movimiento = await ObtenerAsync(movimientoId, ct);
        await movimientoRepositorio.EliminarAsync(movimiento, ct);
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

    /// <summary>
    /// Con <paramref name="propagar"/>, aplica el valor a todos los movimientos de igual moneda
    /// y fecha, sin importar su tipo de cambio histórico previo (FR-023, AC-7.a). Sin propagar,
    /// solo afecta al movimiento editado.
    /// </summary>
    public async Task EditarTipoDeCambioHistoricoAsync(int movimientoId, decimal tipoDeCambio, bool propagar, CancellationToken ct = default)
    {
        if (tipoDeCambio <= 0)
        {
            throw new InvalidOperationException("El tipo de cambio debe ser mayor a cero.");
        }

        var movimiento = await ObtenerAsync(movimientoId, ct);
        var moneda = await monedaRepositorio.ObtenerPorIdAsync(movimiento.MonedaId, ct)
            ?? throw new InvalidOperationException("La moneda del movimiento no existe.");
        if (moneda.EsBase)
        {
            throw new InvalidOperationException("Un movimiento en la moneda base no tiene tipo de cambio histórico.");
        }

        movimiento.TipoDeCambioHistorico = tipoDeCambio;

        if (propagar)
        {
            var mismaMonedaYFecha = await movimientoRepositorio.ListarPorMonedaYFechaAsync(movimiento.MonedaId, movimiento.Fecha, ct);
            foreach (var otro in mismaMonedaYFecha.Where(m => m.Id != movimiento.Id))
            {
                otro.TipoDeCambioHistorico = tipoDeCambio;
            }
        }

        await movimientoRepositorio.GuardarCambiosAsync(ct);
    }

    private async Task<Movimiento> ObtenerAsync(int id, CancellationToken ct) =>
        await movimientoRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new InvalidOperationException("El movimiento no existe.");
}
