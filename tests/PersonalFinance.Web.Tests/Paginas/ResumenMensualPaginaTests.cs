using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Web.Tests.Falsos;
using PaginaResumenMensual = PersonalFinance.Web.Components.Pages.ResumenMensual;

namespace PersonalFinance.Web.Tests.Paginas;

public sealed class ResumenMensualPaginaTests : BunitContext
{
    private readonly RepositorioCategoriaFalso _categorias = new();
    private readonly RepositorioMonedaFalso _monedas = new();
    private readonly RepositorioMovimientoFalso _movimientos = new();
    private readonly Moneda _ars;

    public ResumenMensualPaginaTests()
    {
        _ars = new Moneda { Id = 1, Codigo = "ARS", EsBase = true, Activa = true, TipoDeCambio = null };
        _monedas.Monedas.Add(_ars);

        Services.AddSingleton(new ResumenMensualServicio(_movimientos, _categorias, _monedas));
    }

    private void AgregarMovimientoDelMes(string titulo, decimal monto, TipoMovimiento tipo)
    {
        var categoria = new Categoria { Id = _categorias.Categorias.Count + 1, Titulo = titulo, Descripcion = "d", Activa = true };
        _categorias.Categorias.Add(categoria);

        var hoy = DateTime.Today;
        _movimientos.Movimientos.Add(new Movimiento
        {
            Id = _movimientos.Movimientos.Count + 1,
            MensajeId = 1,
            CategoriaId = categoria.Id,
            MonedaId = _ars.Id,
            Monto = monto,
            Tipo = tipo,
            Fecha = new DateOnly(hoy.Year, hoy.Month, 1),
            TipoDeCambioHistorico = null
        });
    }

    [Fact]
    public void Renderiza_ambos_bloques_con_su_total_general()
    {
        AgregarMovimientoDelMes("Hogar", 500.00m, TipoMovimiento.Egreso);
        AgregarMovimientoDelMes("Sueldo", 1000.00m, TipoMovimiento.Ingreso);

        var componente = Render<PaginaResumenMensual>(
            (ComponentParameterCollectionBuilder<PaginaResumenMensual> parametros) => { });

        Assert.Contains("Ingresos", componente.Markup);
        Assert.Contains("Egresos", componente.Markup);
        Assert.Contains("1,000.00", componente.Markup);
        Assert.Contains("500.00", componente.Markup);
    }

    [Fact]
    public void Pagina_con_menos_de_cuatro_filas_no_muestra_controles_de_navegacion()
    {
        AgregarMovimientoDelMes("Hogar", 500.00m, TipoMovimiento.Egreso);

        var componente = Render<PaginaResumenMensual>(
            (ComponentParameterCollectionBuilder<PaginaResumenMensual> parametros) => { });

        Assert.Empty(componente.FindAll("nav"));
    }

    [Fact]
    public void Bloque_de_cinco_filas_pagina_de_a_cuatro_con_navegacion_independiente()
    {
        for (var i = 0; i < 5; i++)
        {
            AgregarMovimientoDelMes($"Cat{i}", 100.00m, TipoMovimiento.Egreso);
        }

        var componente = Render<PaginaResumenMensual>(
            (ComponentParameterCollectionBuilder<PaginaResumenMensual> parametros) => { });

        var navegacionEgresos = componente.Find("nav[aria-label='Paginación de egresos']");
        Assert.Equal(2, navegacionEgresos.QuerySelectorAll("a").Length);
        Assert.Empty(componente.FindAll("nav[aria-label='Paginación de ingresos']"));

        // El total general de egresos suma las 5 filas, no solo las 4 de la página visible.
        Assert.Contains("500.00", componente.Markup);
    }
}
