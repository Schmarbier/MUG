using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Web.Tests.Falsos;
using PaginaCategorias = PersonalFinance.Web.Components.Pages.Categorias;

namespace PersonalFinance.Web.Tests.Paginas;

public sealed class CategoriasPaginaTests : BunitContext
{
    private readonly RepositorioCategoriaFalso _categorias = new();

    public CategoriasPaginaTests()
    {
        Services.AddSingleton(new CategoriaServicio(_categorias));
    }

    [Fact]
    public void Crear_una_categoria_la_agrega_al_listado_como_activa()
    {
        var componente = Render<PaginaCategorias>(
            (ComponentParameterCollectionBuilder<PaginaCategorias> parametros) => { });

        componente.Find("input[placeholder='Título']").Change("Hogar");
        componente.Find("input[placeholder='Descripción']").Change("gastos del hogar");
        componente.Find("form").Submit();

        Assert.Contains("Hogar", componente.Markup);
        Assert.Contains("activa", componente.Markup);
    }

    [Fact]
    public void Editar_titulo_lo_actualiza_en_el_listado()
    {
        _categorias.Categorias.Add(new() { Id = 1, Titulo = "Hogar", Descripcion = "d", Activa = true });

        var componente = Render<PaginaCategorias>(
            (ComponentParameterCollectionBuilder<PaginaCategorias> parametros) => { });

        componente.Find("button:contains('Editar')").Click();
        componente.Find("tr input").Change("Casa");
        componente.Find("button:contains('Guardar')").Click();

        Assert.Contains("Casa", componente.Markup);
    }

    [Fact]
    public void Eliminar_sin_movimientos_la_quita_del_listado()
    {
        _categorias.Categorias.Add(new() { Id = 1, Titulo = "Hogar", Descripcion = "d", Activa = true });

        var componente = Render<PaginaCategorias>(
            (ComponentParameterCollectionBuilder<PaginaCategorias> parametros) => { });

        componente.Find("button:contains('Eliminar')").Click();

        Assert.DoesNotContain("Hogar", componente.Markup);
    }

    [Fact]
    public void Eliminar_con_movimientos_la_desactiva_y_permite_reactivarla()
    {
        _categorias.Categorias.Add(new() { Id = 1, Titulo = "Ocio", Descripcion = "d", Activa = true });
        _categorias.TieneMovimientosPorCategoria.Add(1);

        var componente = Render<PaginaCategorias>(
            (ComponentParameterCollectionBuilder<PaginaCategorias> parametros) => { });

        componente.Find("button:contains('Eliminar')").Click();
        Assert.Contains("desactivada", componente.Markup);

        componente.Find("button:contains('Reactivar')").Click();
        Assert.Contains("activa", componente.Markup);
        Assert.DoesNotContain("desactivada", componente.Markup);
    }
}
