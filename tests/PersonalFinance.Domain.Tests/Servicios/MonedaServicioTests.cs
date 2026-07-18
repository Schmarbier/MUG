using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Domain.Tests.Falsos;

namespace PersonalFinance.Domain.Tests.Servicios;

public sealed class MonedaServicioTests
{
    private readonly RepositorioMonedaFalso _monedas = new();
    private readonly MonedaServicio _servicio;

    public MonedaServicioTests()
    {
        _servicio = new MonedaServicio(_monedas);
    }

    [Fact]
    public async Task Agregar_con_codigo_unico_y_tipo_de_cambio_mayor_a_cero_la_deja_disponible()
    {
        var moneda = await _servicio.CrearAsync("USD", 1450m);

        Assert.Equal("USD", moneda.Codigo);
        Assert.Equal(1450m, moneda.TipoDeCambio);
        Assert.True(moneda.Activa);
        Assert.False(moneda.EsBase);
    }

    [Fact]
    public async Task Agregar_con_codigo_duplicado_se_rechaza()
    {
        await _servicio.CrearAsync("USD", 1450m);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.CrearAsync("USD", 1500m));
    }

    [Fact]
    public async Task Agregar_con_tipo_de_cambio_menor_o_igual_a_cero_se_rechaza()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.CrearAsync("USD", 0m));
    }

    [Fact]
    public async Task Editar_cotizacion_no_modifica_el_tipo_de_cambio_historico_de_movimientos_existentes()
    {
        var moneda = await _servicio.CrearAsync("USD", 1450m);

        await _servicio.EditarCotizacionAsync(moneda.Id, 1500m);

        Assert.Equal(1500m, moneda.TipoDeCambio);
        // El movimiento ya creado con 1450 no se toca acá: eso lo garantiza que
        // Movimiento.TipoDeCambioHistorico se copia al crear (ClasificacionServicio/MovimientoServicio)
        // y esta operación solo cambia Moneda.TipoDeCambio, nunca itera movimientos.
    }

    [Fact]
    public async Task Editar_cotizacion_a_valor_menor_o_igual_a_cero_se_rechaza_sin_modificar_la_vigente()
    {
        var moneda = await _servicio.CrearAsync("USD", 1450m);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.EditarCotizacionAsync(moneda.Id, 0m));

        Assert.Equal(1450m, moneda.TipoDeCambio);
    }

    [Fact]
    public async Task Eliminar_sin_movimientos_la_borra()
    {
        var moneda = await _servicio.CrearAsync("EUR", 1600m);

        await _servicio.EliminarAsync(moneda.Id);

        Assert.Null(await _monedas.ObtenerPorIdAsync(moneda.Id));
    }

    [Fact]
    public async Task Eliminar_con_movimientos_la_desactiva_preservando_el_tipo_de_cambio_historico()
    {
        var moneda = await _servicio.CrearAsync("USD", 1450m);
        _monedas.TieneMovimientosPorMoneda.Add(moneda.Id);

        await _servicio.EliminarAsync(moneda.Id);

        var resultado = await _monedas.ObtenerPorIdAsync(moneda.Id);
        Assert.NotNull(resultado);
        Assert.False(resultado!.Activa);
        Assert.Equal(1450m, resultado.TipoDeCambio);
    }

    [Fact]
    public async Task ARS_nunca_se_elimina_ni_se_desactiva()
    {
        var ars = new Moneda { Codigo = "ARS", EsBase = true, Activa = true, TipoDeCambio = null };
        await _monedas.AgregarAsync(ars);
        _monedas.TieneMovimientosPorMoneda.Add(ars.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.EliminarAsync(ars.Id));

        var resultado = await _monedas.ObtenerPorIdAsync(ars.Id);
        Assert.True(resultado!.Activa);
    }

    [Fact]
    public async Task Moneda_desactivada_se_excluye_del_listado_activas_y_reactivar_la_devuelve_con_su_tipo_de_cambio()
    {
        var moneda = await _servicio.CrearAsync("USD", 1450m);
        _monedas.TieneMovimientosPorMoneda.Add(moneda.Id);
        await _servicio.EliminarAsync(moneda.Id); // desactivada, no borrada

        var activasTrasDesactivar = await _monedas.ListarActivasAsync();
        Assert.DoesNotContain(activasTrasDesactivar, m => m.Id == moneda.Id);

        await _servicio.ReactivarAsync(moneda.Id);

        var activasTrasReactivar = await _monedas.ListarActivasAsync();
        Assert.Contains(activasTrasReactivar, m => m.Id == moneda.Id && m.TipoDeCambio == 1450m);
    }
}
