using PersonalFinance.Infrastructure.IA;

namespace PersonalFinance.Infrastructure.Tests.IA;

public sealed class MontoArgentinoParserTests
{
    [Theory]
    [InlineData("2000", 2000)]
    [InlineData("2.000", 2000)]
    [InlineData("1.234.567", 1234567)]
    [InlineData("10,22", 10.22)]
    [InlineData("2000.00", 2000.00)]
    [InlineData("2000.5", 2000.5)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("0,01", 0.01)]
    [InlineData("$10", 10)]
    [InlineData("U$S 10", 10)]
    [InlineData("usd 10", 10)]
    [InlineData("10 usd", 10)]
    [InlineData("ARS 1.234,56", 1234.56)]
    public void Parsea_montos_validos_segun_la_convencion_argentina(string texto, decimal esperado)
    {
        var exito = MontoArgentinoParser.TryParsear(texto, out var monto);

        Assert.True(exito);
        Assert.Equal(esperado, monto);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("10,22,33")]
    [InlineData("10,222")]
    [InlineData("1.23.456")]
    [InlineData("10..22")]
    [InlineData(",22")]
    [InlineData("2000.1234")]
    public void Rechaza_formatos_invalidos_o_ambiguos(string? texto)
    {
        var exito = MontoArgentinoParser.TryParsear(texto, out _);

        Assert.False(exito);
    }
}
