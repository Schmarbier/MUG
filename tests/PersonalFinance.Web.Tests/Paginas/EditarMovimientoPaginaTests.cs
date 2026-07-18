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

        Services.AddSingleton(new MovimientoServicio(_movimientos, _monedas));
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
}
