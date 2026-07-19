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

    [Theory]
    [InlineData("3000 alquiler")]
    [InlineData("10,22 dolares en libro")]
    [InlineData("$3000")]
    [InlineData("3000")]
    public void ContieneMonto_detecta_un_numero_en_el_texto_original(string texto)
    {
        Assert.True(MontoArgentinoParser.ContieneMonto(texto));
    }

    [Theory]
    [InlineData("Cine con amigos")]
    [InlineData("")]
    [InlineData("sin numeros aca")]
    public void ContieneMonto_es_false_sin_ningun_numero(string texto)
    {
        Assert.False(MontoArgentinoParser.ContieneMonto(texto));
    }

    [Theory]
    [InlineData("3000 alquiler")]
    [InlineData("Cine con amigos")]
    public void ContieneDescripcion_es_true_cuando_hay_palabras(string texto)
    {
        Assert.True(MontoArgentinoParser.ContieneDescripcion(texto));
    }

    [Theory]
    [InlineData("3000")]
    [InlineData("$3000")]
    [InlineData("1.234,56")]
    [InlineData("")]
    public void ContieneDescripcion_es_false_cuando_es_solo_un_numero(string texto)
    {
        Assert.False(MontoArgentinoParser.ContieneDescripcion(texto));
    }
}
