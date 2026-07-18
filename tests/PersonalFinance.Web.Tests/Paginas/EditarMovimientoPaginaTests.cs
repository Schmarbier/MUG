using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Web.Tests.Falsos;
using PaginaEditarMovimiento = PersonalFinance.Web.Components.Pages.EditarMovimiento;

namespace PersonalFinance.Web.Tests.Paginas;

public sealed class EditarMovimientoPaginaTests : BunitContext
{
    private readonly RepositorioCategoriaFalso _categorias = new();
    private readonly RepositorioMonedaFalso _monedas = new();
    private readonly RepositorioMovimientoFalso _movimientos = new();
    private readonly Categoria _hogar;
    private readonly Categoria _ocio;
    private readonly Moneda _ars;
    private readonly Moneda _usd;
    private readonly Movimiento _movimiento;

    public EditarMovimientoPaginaTests()
    {
        _hogar = new Categoria { Id = 1, Titulo = "Hogar", Descripcion = "d", Activa = true };
        _ocio = new Categoria { Id = 2, Titulo = "Ocio", Descripcion = "d", Activa = true };
        _categorias.Categorias.AddRange([_hogar, _ocio]);

        _ars = new Moneda { Id = 1, Codigo = "ARS", EsBase = true, Activa = true, TipoDeCambio = null };
        _usd = new Moneda { Id = 2, Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1500m };
        _monedas.Monedas.AddRange([_ars, _usd]);

        _movimiento = new Movimiento
        {
            Id = 1,
            MensajeId = 1,
            CategoriaId = _hogar.Id,
            MonedaId = _ars.Id,
            Monto = 2000.00m,
            Tipo = TipoMovimiento.Egreso,
            Fecha = new DateOnly(2026, 7, 5),
            TipoDeCambioHistorico = null
        };
        _movimientos.Movimientos.Add(_movimiento);

        Services.AddSingleton(new MovimientoServicio(_movimientos, _monedas, _categorias));
        Services.AddSingleton<ICategoriaRepositorio>(_categorias);
        Services.AddSingleton<IMonedaRepositorio>(_monedas);
        Services.AddSingleton<IMovimientoRepositorio>(_movimientos);
    }

    [Fact]
    public void Editar_categoria_actualiza_el_movimiento_y_se_refleja_en_el_resumen()
    {
        var componente = Render<PaginaEditarMovimiento>(
            (ComponentParameterCollectionBuilder<PaginaEditarMovimiento> parametros) =>
                parametros.Add(p => p.Id, _movimiento.Id));

        componente.Find("#categoria").Change(_ocio.Id.ToString());
        componente.Find("button:contains('Guardar categoría')").Click();

        Assert.Equal(_ocio.Id, _movimiento.CategoriaId);
    }

    [Fact]
    public void Editar_monto_lo_actualiza()
    {
        var componente = Render<PaginaEditarMovimiento>(
            (ComponentParameterCollectionBuilder<PaginaEditarMovimiento> parametros) =>
                parametros.Add(p => p.Id, _movimiento.Id));

        componente.Find("#monto").Change("2500");
        componente.Find("button:contains('Guardar monto')").Click();

        Assert.Equal(2500.00m, _movimiento.Monto);
    }

    [Fact]
    public void Editar_moneda_registra_el_tipo_de_cambio_vigente()
    {
        var componente = Render<PaginaEditarMovimiento>(
            (ComponentParameterCollectionBuilder<PaginaEditarMovimiento> parametros) =>
                parametros.Add(p => p.Id, _movimiento.Id));

        componente.Find("#moneda").Change(_usd.Id.ToString());
        componente.Find("button:contains('Guardar moneda')").Click();

        Assert.Equal(_usd.Id, _movimiento.MonedaId);
        Assert.Equal(1500m, _movimiento.TipoDeCambioHistorico);
    }

    [Fact]
    public async Task Editar_tipo_mueve_el_movimiento_de_bloque_en_el_resumen_sin_recalculo_manual()
    {
        var resumenServicio = new ResumenMensualServicio(_movimientos, _categorias, _monedas);
        var hoy = DateTime.Today;
        _movimiento.Fecha = new DateOnly(hoy.Year, hoy.Month, 15);

        var componente = Render<PaginaEditarMovimiento>(
            (ComponentParameterCollectionBuilder<PaginaEditarMovimiento> parametros) =>
                parametros.Add(p => p.Id, _movimiento.Id));

        componente.Find("#tipo").Change(nameof(TipoMovimiento.Ingreso));
        componente.Find("button:contains('Guardar tipo')").Click();

        var resumen = await resumenServicio.ObtenerResumenAsync(hoy.Year, hoy.Month, 1, 1);
        Assert.Empty(resumen.Egresos.Filas);
        Assert.Single(resumen.Ingresos.Filas);
        Assert.Equal(2000.00m, resumen.Ingresos.Filas[0].EquivalenteEnBase);
    }

    [Fact]
    public void Editar_tipo_de_cambio_historico_pide_confirmacion_antes_de_propagar()
    {
        var fecha = new DateOnly(2026, 7, 10);
        var editado = new Movimiento { Id = 2, MensajeId = 2, CategoriaId = _hogar.Id, MonedaId = _usd.Id, Monto = 100.00m, Tipo = TipoMovimiento.Egreso, Fecha = fecha, TipoDeCambioHistorico = 1500m };
        var otro = new Movimiento { Id = 3, MensajeId = 3, CategoriaId = _hogar.Id, MonedaId = _usd.Id, Monto = 200.00m, Tipo = TipoMovimiento.Egreso, Fecha = fecha, TipoDeCambioHistorico = 1500m };
        _movimientos.Movimientos.AddRange([editado, otro]);

        var componente = Render<PaginaEditarMovimiento>(
            (ComponentParameterCollectionBuilder<PaginaEditarMovimiento> parametros) =>
                parametros.Add(p => p.Id, editado.Id));

        componente.Find("#tipoDeCambioHistorico").Change("1450");
        componente.Find("button:contains('Guardar tipo de cambio histórico')").Click();

        Assert.Contains("¿Aplicar este tipo de cambio también a los demás movimientos", componente.Markup);
        Assert.Equal(1500m, editado.TipoDeCambioHistorico); // todavía no se guardó, falta confirmar

        componente.Find("button:contains('Sí, aplicar a los demás')").Click();

        Assert.Equal(1450m, editado.TipoDeCambioHistorico);
        Assert.Equal(1450m, otro.TipoDeCambioHistorico);
    }

    [Fact]
    public void Editar_tipo_de_cambio_historico_sin_confirmar_no_propaga()
    {
        var fecha = new DateOnly(2026, 7, 10);
        var editado = new Movimiento { Id = 2, MensajeId = 2, CategoriaId = _hogar.Id, MonedaId = _usd.Id, Monto = 100.00m, Tipo = TipoMovimiento.Egreso, Fecha = fecha, TipoDeCambioHistorico = 1500m };
        var otro = new Movimiento { Id = 3, MensajeId = 3, CategoriaId = _hogar.Id, MonedaId = _usd.Id, Monto = 200.00m, Tipo = TipoMovimiento.Egreso, Fecha = fecha, TipoDeCambioHistorico = 1500m };
        _movimientos.Movimientos.AddRange([editado, otro]);

        var componente = Render<PaginaEditarMovimiento>(
            (ComponentParameterCollectionBuilder<PaginaEditarMovimiento> parametros) =>
                parametros.Add(p => p.Id, editado.Id));

        componente.Find("#tipoDeCambioHistorico").Change("1450");
        componente.Find("button:contains('Guardar tipo de cambio histórico')").Click();
        componente.Find("button:contains('No, solo este movimiento')").Click();

        Assert.Equal(1450m, editado.TipoDeCambioHistorico);
        Assert.Equal(1500m, otro.TipoDeCambioHistorico);
    }

    [Fact]
    public void Un_movimiento_en_moneda_base_no_ofrece_editar_tipo_de_cambio_historico()
    {
        var componente = Render<PaginaEditarMovimiento>(
            (ComponentParameterCollectionBuilder<PaginaEditarMovimiento> parametros) =>
                parametros.Add(p => p.Id, _movimiento.Id));

        Assert.Empty(componente.FindAll("#tipoDeCambioHistorico"));
    }

    [Fact]
    public void Editar_fecha_reasigna_el_movimiento_de_mes()
    {
        var componente = Render<PaginaEditarMovimiento>(
            (ComponentParameterCollectionBuilder<PaginaEditarMovimiento> parametros) =>
                parametros.Add(p => p.Id, _movimiento.Id));

        componente.Find("#fecha").Change("2026-06-30");
        componente.Find("button:contains('Guardar fecha')").Click();

        Assert.Equal(new DateOnly(2026, 6, 30), _movimiento.Fecha);
    }

    [Fact]
    public void Eliminar_el_movimiento_lo_borra_definitivamente()
    {
        var componente = Render<PaginaEditarMovimiento>(
            (ComponentParameterCollectionBuilder<PaginaEditarMovimiento> parametros) =>
                parametros.Add(p => p.Id, _movimiento.Id));

        componente.Find("button:contains('Eliminar movimiento')").Click();

        Assert.Empty(_movimientos.Movimientos);
        Assert.Contains("Movimiento no encontrado", componente.Markup);
    }
}
