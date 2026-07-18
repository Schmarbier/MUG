using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Web.Tests.Falsos;
using PaginaMonedas = PersonalFinance.Web.Components.Pages.Monedas;

namespace PersonalFinance.Web.Tests.Paginas;

public sealed class MonedasPaginaTests : BunitContext
{
    private readonly RepositorioMonedaFalso _monedas = new();

    public MonedasPaginaTests()
    {
        _monedas.Monedas.Add(new() { Id = 1, Codigo = "ARS", EsBase = true, Activa = true, TipoDeCambio = null });
        Services.AddSingleton(new MonedaServicio(_monedas));
    }

    [Fact]
    public void Agregar_una_moneda_la_incluye_en_el_listado()
    {
        var componente = Render<PaginaMonedas>(
            (ComponentParameterCollectionBuilder<PaginaMonedas> parametros) => { });

        componente.Find("input[placeholder='Código']").Change("USD");
        componente.Find("input[placeholder='Tipo de cambio']").Change("1450");
        componente.Find("form").Submit();

        Assert.Contains("USD", componente.Markup);
        Assert.Contains("1,450.00", componente.Markup);
    }

    [Fact]
    public void Editar_cotizacion_la_actualiza()
    {
        _monedas.Monedas.Add(new() { Id = 2, Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1450m });

        var componente = Render<PaginaMonedas>(
            (ComponentParameterCollectionBuilder<PaginaMonedas> parametros) => { });

        componente.Find("button:contains('Editar cotización')").Click();
        componente.Find("tr input").Change("1500");
        componente.Find("button:contains('Guardar')").Click();

        Assert.Contains("1,500.00", componente.Markup);
    }

    [Fact]
    public void Eliminar_con_movimientos_la_desactiva_y_permite_reactivarla()
    {
        _monedas.Monedas.Add(new() { Id = 2, Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1450m });
        _monedas.TieneMovimientosPorMoneda.Add(2);

        var componente = Render<PaginaMonedas>(
            (ComponentParameterCollectionBuilder<PaginaMonedas> parametros) => { });

        componente.Find("button:contains('Eliminar')").Click();
        Assert.Contains("desactivada", componente.Markup);

        componente.Find("button:contains('Reactivar')").Click();
        Assert.DoesNotContain("desactivada", componente.Markup);
    }

    [Fact]
    public void La_moneda_base_no_ofrece_eliminar_ni_reactivar()
    {
        var componente = Render<PaginaMonedas>(
            (ComponentParameterCollectionBuilder<PaginaMonedas> parametros) => { });

        Assert.Contains("moneda base", componente.Markup);
        Assert.Empty(componente.FindAll("button:contains('Eliminar')"));
    }
}
