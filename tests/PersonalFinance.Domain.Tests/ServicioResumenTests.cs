using PersonalFinance.Domain;

namespace PersonalFinance.Domain.Tests;

public class ServicioResumenTests
{
    private static MovimientoResumen Egreso(string cat, string moneda, decimal monto, decimal? tc = null)
        => new(TipoMovimiento.Egreso, cat, moneda, monto, tc);

    private static MovimientoResumen Ingreso(string cat, string moneda, decimal monto, decimal? tc = null)
        => new(TipoMovimiento.Ingreso, cat, moneda, monto, tc);

    [Fact] // AC-10: 3 egresos ARS de Hogar (2000+3000+1500) -> fila "Hogar — ARS" = 6500
    public void Agrupa_y_suma_por_categoria_y_moneda()
    {
        var resumen = ServicioResumen.Construir(
        [
            Egreso("Hogar", "ARS", 2000m),
            Egreso("Hogar", "ARS", 3000m),
            Egreso("Hogar", "ARS", 1500m),
        ]);

        var fila = Assert.Single(resumen.Egresos);
        Assert.Equal("Hogar", fila.CategoriaTitulo);
        Assert.Equal("ARS", fila.MonedaCodigo);
        Assert.Equal(6500m, fila.Total);
        Assert.Empty(resumen.Ingresos);
    }

    [Fact] // AC-41: ingreso y egreso de la misma categoria no se netean
    public void Ingreso_y_egreso_de_misma_categoria_no_se_netean()
    {
        var resumen = ServicioResumen.Construir(
        [
            Egreso("Ahorro", "ARS", 800m),
            Ingreso("Ahorro", "ARS", 800m),
        ]);

        var egreso = Assert.Single(resumen.Egresos);
        var ingreso = Assert.Single(resumen.Ingresos);
        Assert.Equal(800m, egreso.Total);
        Assert.Equal(800m, ingreso.Total);
    }

    [Fact] // AC-31: Ahorro USD 800 con tc historico 1450 -> equivalente 1.160.000 ARS, separado de ARS
    public void Moneda_extranjera_calcula_equivalente_historico_en_ars()
    {
        var resumen = ServicioResumen.Construir(
        [
            Egreso("Ahorro", "USD", 800m, tc: 1450m),
            Egreso("Ahorro", "ARS", 500m),
        ]);

        var filaUsd = Assert.Single(resumen.Egresos, f => f.MonedaCodigo == "USD");
        Assert.Equal(800m, filaUsd.Total);
        Assert.Equal(1_160_000m, filaUsd.EquivalenteArs);
        Assert.Contains(resumen.Egresos, f => f.MonedaCodigo == "ARS");
    }

    [Fact] // AC-11: 10 filas se paginan de a 4 -> 3 paginas (4, 4, 2)
    public void Paginacion_de_a_cuatro()
    {
        var filas = Enumerable.Range(1, 10)
            .Select(i => new FilaResumen($"Cat{i:D2}", "ARS", i, i))
            .ToList();

        Assert.Equal(3, Paginador.TotalPaginas(filas.Count));
        Assert.Equal(4, Paginador.Pagina(filas, 1).Count);
        Assert.Equal(4, Paginador.Pagina(filas, 2).Count);
        Assert.Equal(2, Paginador.Pagina(filas, 3).Count);
    }
}
