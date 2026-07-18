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

    [Fact]
    public async Task Editar_tipo_de_cambio_historico_con_confirmacion_propaga_a_movimientos_de_igual_moneda_y_fecha()
    {
        _monedas.Monedas.Add(new Moneda { Id = 1, Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1500m });
        var fecha = new DateOnly(2026, 7, 10);
        var editado = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 100.00m, TipoMovimiento.Egreso, tipoDeCambioHistorico: 1500m);
        editado.Fecha = fecha;
        var otro1 = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 200.00m, TipoMovimiento.Egreso, tipoDeCambioHistorico: 1500m);
        otro1.Fecha = fecha;
        var otro2 = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 300.00m, TipoMovimiento.Egreso, tipoDeCambioHistorico: 1480m);
        otro2.Fecha = fecha;

        await _servicio.EditarTipoDeCambioHistoricoAsync(editado.Id, 1450m, propagar: true);

        Assert.Equal(1450m, editado.TipoDeCambioHistorico);
        Assert.Equal(1450m, otro1.TipoDeCambioHistorico);
        Assert.Equal(1450m, otro2.TipoDeCambioHistorico); // se aplica sin importar su valor previo (AC-7.a)
    }

    [Fact]
    public async Task Editar_tipo_de_cambio_historico_sin_confirmar_solo_afecta_al_editado()
    {
        _monedas.Monedas.Add(new Moneda { Id = 1, Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1500m });
        var fecha = new DateOnly(2026, 7, 10);
        var editado = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 100.00m, TipoMovimiento.Egreso, tipoDeCambioHistorico: 1500m);
        editado.Fecha = fecha;
        var otro = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 200.00m, TipoMovimiento.Egreso, tipoDeCambioHistorico: 1500m);
        otro.Fecha = fecha;

        await _servicio.EditarTipoDeCambioHistoricoAsync(editado.Id, 1450m, propagar: false);

        Assert.Equal(1450m, editado.TipoDeCambioHistorico);
        Assert.Equal(1500m, otro.TipoDeCambioHistorico);
    }

    [Fact]
    public async Task Editar_tipo_de_cambio_historico_a_valor_menor_o_igual_a_cero_se_rechaza_sin_modificar_ni_propagar()
    {
        _monedas.Monedas.Add(new Moneda { Id = 1, Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1500m });
        var movimiento = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 100.00m, TipoMovimiento.Egreso, tipoDeCambioHistorico: 1500m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _servicio.EditarTipoDeCambioHistoricoAsync(movimiento.Id, 0m, propagar: true));

        Assert.Equal(1500m, movimiento.TipoDeCambioHistorico);
    }

    [Fact]
    public async Task Editar_tipo_de_cambio_historico_de_un_movimiento_en_moneda_base_se_rechaza()
    {
        _monedas.Monedas.Add(new Moneda { Id = 1, Codigo = "ARS", EsBase = true, Activa = true, TipoDeCambio = null });
        var movimiento = AgregarMovimiento(categoriaId: 1, monedaId: 1, monto: 100.00m, TipoMovimiento.Egreso);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _servicio.EditarTipoDeCambioHistoricoAsync(movimiento.Id, 1500m, propagar: false));

        Assert.Null(movimiento.TipoDeCambioHistorico);
    }
}
