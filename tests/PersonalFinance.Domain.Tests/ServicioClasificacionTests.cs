using PersonalFinance.Domain;

namespace PersonalFinance.Domain.Tests;

public class ServicioClasificacionTests
{
    // Fakes: el agente devuelve una extracción fija (simula la salida del LLM),
    // los repos devuelven datos controlados. Cero Ollama, cero DB.
    private sealed class AgenteFake(ExtraccionMovimiento resultado) : IAgenteClasificador
    {
        public Task<ExtraccionMovimiento> ExtraerAsync(
            string texto, IReadOnlyList<Categoria> categoriasActivas, CancellationToken ct = default)
            => Task.FromResult(resultado);
    }

    private sealed class CategoriasFake(params Categoria[] activas) : IRepositorioCategorias
    {
        public Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Categoria>>(activas);
    }

    private sealed class MonedasFake(params Moneda[] monedas) : IRepositorioMonedas
    {
        public Task<Moneda?> ObtenerAsync(string codigo, CancellationToken ct = default)
            => Task.FromResult(Array.Find(monedas, m => m.Codigo == codigo));
    }

    private static readonly Categoria Sueldo = new() { Id = 2, Titulo = "Sueldo", Activa = true };
    private static readonly Categoria Hogar = new() { Id = 1, Titulo = "Hogar", Activa = true };
    private static readonly Categoria Ahorro = new() { Id = 3, Titulo = "Ahorro", Activa = true };

    private static ServicioClasificacion Servicio(ExtraccionMovimiento extraccion, params Moneda[] monedas)
        => new(new AgenteFake(extraccion), new CategoriasFake(Sueldo, Hogar, Ahorro), new MonedasFake(monedas));

    private static Mensaje MensajeCon(string texto) => new() { Id = 10, Texto = texto, FechaMensaje = new DateTime(2026, 7, 10) };

    [Fact] // AC-05: "$10.000 ingreso" sin moneda -> ingreso, ARS, tc historico no aplica
    public async Task Ingreso_en_ARS_por_defecto_crea_movimiento()
    {
        var servicio = Servicio(new ExtraccionMovimiento(10000m, "ingreso", TipoMovimiento.Ingreso, "Sueldo", null));

        var r = await servicio.ClasificarAsync(MensajeCon("$10.000 ingreso"));

        Assert.True(r.EsExito);
        Assert.Equal(TipoMovimiento.Ingreso, r.Movimiento!.Tipo);
        Assert.Equal(10000m, r.Movimiento.Monto);
        Assert.Equal("ARS", r.Movimiento.MonedaCodigo);
        Assert.Null(r.Movimiento.TipoCambioHistorico);
        Assert.Equal(Sueldo.Id, r.Movimiento.CategoriaId);
    }

    [Fact] // AC-06: "$2.000 comida casa" -> egreso, ARS
    public async Task Egreso_en_ARS_por_defecto_crea_movimiento()
    {
        var servicio = Servicio(new ExtraccionMovimiento(2000m, "comida casa", TipoMovimiento.Egreso, "Hogar", null));

        var r = await servicio.ClasificarAsync(MensajeCon("$2.000 comida casa"));

        Assert.True(r.EsExito);
        Assert.Equal(TipoMovimiento.Egreso, r.Movimiento!.Tipo);
        Assert.Equal("ARS", r.Movimiento.MonedaCodigo);
        Assert.Equal(Hogar.Id, r.Movimiento.CategoriaId);
    }

    [Fact] // AC-07: sin monto -> error "no contiene monto"
    public async Task Sin_monto_marca_error()
    {
        var servicio = Servicio(new ExtraccionMovimiento(null, "algo", TipoMovimiento.Egreso, "Hogar", null));

        var r = await servicio.ClasificarAsync(MensajeCon("comida casa"));

        Assert.False(r.EsExito);
        Assert.Equal("no contiene monto", r.MotivoError);
    }

    [Fact] // AC-08: monto sin descripcion -> error "no contiene descripcion"
    public async Task Con_monto_pero_sin_descripcion_marca_error()
    {
        var servicio = Servicio(new ExtraccionMovimiento(2000m, "   ", TipoMovimiento.Egreso, "Hogar", null));

        var r = await servicio.ClasificarAsync(MensajeCon("$2.000"));

        Assert.False(r.EsExito);
        Assert.Equal("no contiene descripcion", r.MotivoError);
    }

    [Fact] // AC-30: moneda no cargada -> error "moneda no soportada"
    public async Task Moneda_no_cargada_marca_error()
    {
        var servicio = Servicio(new ExtraccionMovimiento(100m, "viaje", TipoMovimiento.Egreso, "Ocio", "EUR"));

        var r = await servicio.ClasificarAsync(MensajeCon("100 EUR viaje"));

        Assert.False(r.EsExito);
        Assert.Equal("moneda no soportada", r.MotivoError);
    }

    [Fact] // AC-28: "800 USD ahorro" con USD tc=1450 -> monto 800, USD, egreso, tc historico 1450
    public async Task Moneda_extranjera_congela_tipo_de_cambio_historico()
    {
        var usd = new Moneda { Codigo = "USD", TipoCambio = 1450m, EsBase = false };
        var servicio = Servicio(new ExtraccionMovimiento(800m, "ahorro", TipoMovimiento.Egreso, "Ahorro", "USD"), usd);

        var r = await servicio.ClasificarAsync(MensajeCon("800 USD ahorro"));

        Assert.True(r.EsExito);
        Assert.Equal(800m, r.Movimiento!.Monto);
        Assert.Equal("USD", r.Movimiento.MonedaCodigo);
        Assert.Equal(TipoMovimiento.Egreso, r.Movimiento.Tipo);
        Assert.Equal(1450m, r.Movimiento.TipoCambioHistorico);
    }
}
