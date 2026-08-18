using PersonalFinance.Infrastructure.Telegram;
using Xunit;

namespace PersonalFinance.Infrastructure.Tests;

public class OpcionesTelegramTests
{
    private const string Secreto = "AAHscodigosecretodelbot1234567890abc";

    // Valida M-03: el token no llega nunca a un log. Un record imprime todas sus propiedades
    // por defecto, así que sin este override alcanzaría con interpolar las opciones en una línea
    // de log para filtrar el secreto.
    [Fact]
    public void OpcionesTelegram_ToString_EnmascaraElToken()
    {
        var opciones = new OpcionesTelegram($"123456789:{Secreto}", ChatAutorizado: 555);

        var texto = opciones.ToString();

        Assert.DoesNotContain(Secreto, texto, StringComparison.Ordinal);
        Assert.Contains("123456789:***", texto, StringComparison.Ordinal);
    }

    // El chat autorizado no es secreto y sirve para diagnosticar por qué el bot no ingiere nada.
    [Fact]
    public void OpcionesTelegram_ToString_ConservaElChatAutorizado()
    {
        var opciones = new OpcionesTelegram($"123456789:{Secreto}", ChatAutorizado: 555);

        Assert.Contains("555", opciones.ToString(), StringComparison.Ordinal);
    }
}
