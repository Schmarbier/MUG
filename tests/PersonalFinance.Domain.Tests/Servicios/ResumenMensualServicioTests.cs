using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Domain.Tests.Falsos;

namespace PersonalFinance.Domain.Tests.Servicios;

public sealed class ResumenMensualServicioTests
{
    private readonly RepositorioCategoriaFalso _categorias = new();
    private readonly RepositorioMonedaFalso _monedas = new();
    private readonly RepositorioMovimientoFalso _movimientos = new();
    private readonly ResumenMensualServicio _servicio;

    public ResumenMensualServicioTests()
    {
        _servicio = new ResumenMensualServicio(_movimientos, _categorias, _monedas);
    }

    private Categoria AgregarCategoria(string titulo)
    {
        var categoria = new Categoria { Titulo = titulo, Descripcion = "d", Activa = true };
        categoria.Id = _categorias.Categorias.Count + 1;
        _categorias.Categorias.Add(categoria);
        return categoria;
    }

    private Moneda AgregarMoneda(string codigo, bool esBase)
    {
        var moneda = new Moneda { Codigo = codigo, EsBase = esBase, Activa = true, TipoDeCambio = esBase ? null : 1000m };
        moneda.Id = _monedas.Monedas.Count + 1;
        _monedas.Monedas.Add(moneda);
        return moneda;
    }

    private void AgregarMovimiento(
        Categoria categoria, Moneda moneda, decimal monto, TipoMovimiento tipo,
        decimal? tipoDeCambioHistorico = null, DateOnly? fecha = null)
    {
        _movimientos.Movimientos.Add(new Movimiento
        {
            Id = _movimientos.Movimientos.Count + 1,
            MensajeId = 1,
            CategoriaId = categoria.Id,
            MonedaId = moneda.Id,
            Monto = monto,
            Tipo = tipo,
            Fecha = fecha ?? new DateOnly(2026, 7, 15),
            TipoDeCambioHistorico = tipoDeCambioHistorico
        });
    }

    [Fact]
    public async Task Tres_egresos_de_igual_categoria_y_moneda_agrupan_en_una_fila_con_el_total_sumado()
    {
        var hogar = AgregarCategoria("Hogar");
        var ars = AgregarMoneda("ARS", esBase: true);
        AgregarMovimiento(hogar, ars, 100.00m, TipoMovimiento.Egreso);
        AgregarMovimiento(hogar, ars, 200.00m, TipoMovimiento.Egreso);
        AgregarMovimiento(hogar, ars, 300.00m, TipoMovimiento.Egreso);

        var resumen = await _servicio.ObtenerResumenAsync(2026, 7, 1, 1);

        var fila = Assert.Single(resumen.Egresos.Filas);
        Assert.Equal(600.00m, fila.TotalEnMoneda);
        Assert.Equal(600.00m, fila.EquivalenteEnBase);
    }

    [Fact]
    public async Task Egreso_e_ingreso_de_igual_monto_y_categoria_no_se_netean_entre_bloques()
    {
        var hogar = AgregarCategoria("Hogar");
        var ars = AgregarMoneda("ARS", esBase: true);
        AgregarMovimiento(hogar, ars, 500.00m, TipoMovimiento.Egreso);
        AgregarMovimiento(hogar, ars, 500.00m, TipoMovimiento.Ingreso);

        var resumen = await _servicio.ObtenerResumenAsync(2026, 7, 1, 1);

        Assert.Equal(500.00m, resumen.Egresos.TotalGeneral);
        Assert.Equal(500.00m, resumen.Ingresos.TotalGeneral);
    }

    [Fact]
    public async Task Equivalente_de_fila_en_moneda_extranjera_suma_equivalentes_individuales()
    {
        var hogar = AgregarCategoria("Hogar");
        var usd = AgregarMoneda("USD", esBase: false);
        AgregarMovimiento(hogar, usd, 10.00m, TipoMovimiento.Egreso, tipoDeCambioHistorico: 1000m);
        AgregarMovimiento(hogar, usd, 10.00m, TipoMovimiento.Egreso, tipoDeCambioHistorico: 1200m);

        var resumen = await _servicio.ObtenerResumenAsync(2026, 7, 1, 1);

        var fila = Assert.Single(resumen.Egresos.Filas);
        Assert.Equal(20.00m, fila.TotalEnMoneda);
        Assert.Equal(22000.00m, fila.EquivalenteEnBase); // 10*1000 + 10*1200, no un tipo único sobre el total
    }

    [Fact]
    public async Task Redondeo_unico_con_empate_hacia_arriba_no_bancario()
    {
        var hogar = AgregarCategoria("Hogar");
        var ars = AgregarMoneda("ARS", esBase: true);
        AgregarMovimiento(hogar, ars, 1465.0555m, TipoMovimiento.Egreso);
        AgregarMovimiento(hogar, ars, 1465.0555m, TipoMovimiento.Egreso);

        var resumen = await _servicio.ObtenerResumenAsync(2026, 7, 1, 1);

        var fila = Assert.Single(resumen.Egresos.Filas);
        Assert.Equal(2930.11m, fila.EquivalenteEnBase);
        Assert.Equal(2930.11m, resumen.Egresos.TotalGeneral);
    }

    [Fact]
    public async Task Filas_ordenan_descendente_con_desempate_alfabetico_y_paginan_de_a_cuatro_de_forma_deterministica()
    {
        var monedaBase = AgregarMoneda("ARS", esBase: true);
        var alfa = AgregarCategoria("Alfa");
        var beta = AgregarCategoria("Beta");
        var gama = AgregarCategoria("Gama");
        var delta = AgregarCategoria("Delta");
        var epsilon = AgregarCategoria("Epsilon");

        AgregarMovimiento(alfa, monedaBase, 100.00m, TipoMovimiento.Egreso);
        AgregarMovimiento(beta, monedaBase, 100.00m, TipoMovimiento.Egreso); // empate con Alfa -> desempate alfabético
        AgregarMovimiento(gama, monedaBase, 300.00m, TipoMovimiento.Egreso);
        AgregarMovimiento(delta, monedaBase, 200.00m, TipoMovimiento.Egreso);
        AgregarMovimiento(epsilon, monedaBase, 50.00m, TipoMovimiento.Egreso);

        var primeraConsulta = await _servicio.ObtenerResumenAsync(2026, 7, 1, 1);
        var segundaConsulta = await _servicio.ObtenerResumenAsync(2026, 7, 1, 1);

        Assert.Equal(2, primeraConsulta.Egresos.TotalPaginas);
        Assert.Equal(4, primeraConsulta.Egresos.Filas.Count);
        Assert.Equal(["Gama", "Delta", "Alfa", "Beta"], primeraConsulta.Egresos.Filas.Select(f => f.Categoria));

        var segundaPagina = await _servicio.ObtenerResumenAsync(2026, 7, 1, paginaEgresos: 2);
        Assert.Equal(["Epsilon"], segundaPagina.Egresos.Filas.Select(f => f.Categoria));

        Assert.Equal(
            primeraConsulta.Egresos.Filas.Select(f => f.Categoria),
            segundaConsulta.Egresos.Filas.Select(f => f.Categoria));
    }

    [Fact]
    public async Task Total_general_del_bloque_suma_todas_las_filas_del_mes_y_no_varia_al_paginar()
    {
        var monedaBase = AgregarMoneda("ARS", esBase: true);
        for (var i = 0; i < 5; i++)
        {
            var categoria = AgregarCategoria($"Cat{i}");
            AgregarMovimiento(categoria, monedaBase, 100.00m, TipoMovimiento.Egreso);
        }

        var pagina1 = await _servicio.ObtenerResumenAsync(2026, 7, 1, 1);
        var pagina2 = await _servicio.ObtenerResumenAsync(2026, 7, 1, paginaEgresos: 2);

        Assert.Equal(500.00m, pagina1.Egresos.TotalGeneral);
        Assert.Equal(500.00m, pagina2.Egresos.TotalGeneral);
    }

    [Fact]
    public async Task Mes_sin_movimientos_muestra_ambos_bloques_con_totales_en_cero()
    {
        AgregarMoneda("ARS", esBase: true);

        var resumen = await _servicio.ObtenerResumenAsync(2026, 7, 1, 1);

        Assert.Empty(resumen.Ingresos.Filas);
        Assert.Empty(resumen.Egresos.Filas);
        Assert.Equal(0m, resumen.Ingresos.TotalGeneral);
        Assert.Equal(0m, resumen.Egresos.TotalGeneral);
    }

    [Fact]
    public async Task Bloque_con_menos_de_cuatro_filas_produce_una_unica_pagina()
    {
        var monedaBase = AgregarMoneda("ARS", esBase: true);
        var hogar = AgregarCategoria("Hogar");
        AgregarMovimiento(hogar, monedaBase, 100.00m, TipoMovimiento.Egreso);

        var resumen = await _servicio.ObtenerResumenAsync(2026, 7, 1, 1);

        Assert.Equal(1, resumen.Egresos.TotalPaginas);
        Assert.Equal(1, resumen.Egresos.PaginaActual);
    }
}
