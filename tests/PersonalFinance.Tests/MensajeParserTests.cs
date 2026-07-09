using PersonalFinance.Bot.Services;

namespace PersonalFinance.Tests;

public class MensajeParserTests
{
    private readonly MensajeParser _parser = new();

    [Fact]
    public void Extrae_monto_y_descripcion()
    {
        var r = _parser.Parse("$2.000 comida casa");

        Assert.True(r.EsValido);
        Assert.Equal(2000m, r.Monto);
        Assert.Equal("comida casa", r.Descripcion);
    }

    [Fact]
    public void Extrae_monto_con_una_sola_palabra_de_descripcion()
    {
        var r = _parser.Parse("$10.000 ingreso");

        Assert.True(r.EsValido);
        Assert.Equal(10000m, r.Monto);
        Assert.Equal("ingreso", r.Descripcion);
    }

    [Fact]
    public void Soporta_decimales_y_monto_sin_signo_pesos()
    {
        var r = _parser.Parse("2.000,50 nafta");

        Assert.True(r.EsValido);
        Assert.Equal(2000.50m, r.Monto);
        Assert.Equal("nafta", r.Descripcion);
    }

    // AC-05: mensaje sin monto -> error "no contiene monto".
    [Fact]
    public void Sin_monto_falla_con_motivo()
    {
        var r = _parser.Parse("comida casa");

        Assert.False(r.EsValido);
        Assert.Equal(MensajeParser.MotivoNoContieneMonto, r.Motivo);
    }

    // AC-06: mensaje con monto pero sin descripcion -> error "no contiene descripcion".
    [Fact]
    public void Con_monto_pero_sin_descripcion_falla_con_motivo()
    {
        var r = _parser.Parse("$500");

        Assert.False(r.EsValido);
        Assert.Equal(MensajeParser.MotivoNoContieneDescripcion, r.Motivo);
    }
}
