using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Domain.Tests.Falsos;

namespace PersonalFinance.Domain.Tests.Servicios;

public sealed class MovimientoServicioTests
{
    private readonly RepositorioMovimientoFalso _movimientos = new();
    private readonly RepositorioMonedaFalso _monedas = new();
    private readonly MovimientoServicio _servicio;

    public MovimientoServicioTests()
    {
        _servicio = new MovimientoServicio(_movimientos, _monedas);
    }

    private Movimiento AgregarMovimiento(int categoriaId, int monedaId, decimal monto, TipoMovimiento tipo, decimal? tipoDeCambioHistorico = null)
    {
        var movimiento = new Movimiento
        {
            Id = _movimientos.Movimientos.Count + 1,
            MensajeId = 1,
            CategoriaId = categoriaId,
            MonedaId = monedaId,
            Monto = monto,
            Tipo = tipo,
            Fecha = new DateOnly(2026, 7, 5),
            TipoDeCambioHistorico = tipoDeCambioHistorico
        };
        _movimientos.Movimientos.Add(movimiento);
        return movimiento;
    }

    [Fact]
    public async Task Editar_categoria_lo_actualiza_sin_afectar_otros_campos()
    {
        var movimiento = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 2000.00m, TipoMovimiento.Egreso);

        await _servicio.EditarCategoriaAsync(movimiento.Id, categoriaId: 2);

        Assert.Equal(2, movimiento.CategoriaId);
        Assert.Equal(2000.00m, movimiento.Monto);
    }

    [Fact]
    public async Task Editar_monto_lo_actualiza_sin_afectar_otros_campos()
    {
        var movimiento = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 2000.00m, TipoMovimiento.Egreso);

        await _servicio.EditarMontoAsync(movimiento.Id, 2500.00m);

        Assert.Equal(2500.00m, movimiento.Monto);
        Assert.Equal(1, movimiento.CategoriaId);
    }

    [Fact]
    public async Task Editar_monto_a_valor_menor_o_igual_a_cero_se_rechaza_sin_modificar_el_movimiento()
    {
        var movimiento = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 2000.00m, TipoMovimiento.Egreso);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.EditarMontoAsync(movimiento.Id, 0m));

        Assert.Equal(2000.00m, movimiento.Monto);
    }

    [Fact]
    public async Task Editar_moneda_registra_el_tipo_de_cambio_vigente_de_la_nueva_moneda()
    {
        _monedas.Monedas.Add(new Moneda { Id = 1, Codigo = "ARS", EsBase = true, Activa = true, TipoDeCambio = null });
        _monedas.Monedas.Add(new Moneda { Id = 2, Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1500m });
        var movimiento = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 100.00m, TipoMovimiento.Egreso);

        await _servicio.EditarMonedaAsync(movimiento.Id, monedaId: 2);

        Assert.Equal(2, movimiento.MonedaId);
        Assert.Equal(1500m, movimiento.TipoDeCambioHistorico);
    }

    [Fact]
    public async Task Editar_tipo_mueve_de_bloque_sin_alterar_monto_moneda_ni_tipo_de_cambio_historico()
    {
        _monedas.Monedas.Add(new Moneda { Id = 1, Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1500m });
        var movimiento = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 10000.00m, TipoMovimiento.Egreso, tipoDeCambioHistorico: 1450m);

        await _servicio.EditarTipoAsync(movimiento.Id, TipoMovimiento.Ingreso);

        Assert.Equal(TipoMovimiento.Ingreso, movimiento.Tipo);
        Assert.Equal(10000.00m, movimiento.Monto);
        Assert.Equal(1, movimiento.MonedaId);
        Assert.Equal(1450m, movimiento.TipoDeCambioHistorico);
    }
}
