using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Domain.CasosDeUso;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Infrastructure.Telegram;
using Xunit;

namespace PersonalFinance.Infrastructure.Tests;

public class AgregarTelegramExtensionsTests
{
    private const string Token = "123456789:AAHscodigosecretodelbot1234567890abc";

    // Sad path del error documentado: sin token el proceso no arranca. La validación vive acá y
    // no en Program.cs, que sólo lee configuración y pasa primitivos.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarTelegram_TokenVacio_LanzaArgumentException(string token)
    {
        var excepcion = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AgregarTelegram(token, chatAutorizado: 555));

        Assert.Equal("token", excepcion.ParamName);
    }

    // Sad path: un token con formato inválido falla al arrancar, no en la primera lectura.
    [Theory]
    [InlineData("sin-dos-puntos")]
    [InlineData(":solo-secreto")]
    [InlineData("abc:secreto")]
    public void AgregarTelegram_TokenConFormatoInvalido_LanzaArgumentException(string token)
    {
        var excepcion = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AgregarTelegram(token, chatAutorizado: 555));

        Assert.Equal("token", excepcion.ParamName);
    }

    // Valida M-03: el mensaje de error de un arranque fallido termina en un log, así que no
    // puede llevar el token adentro.
    [Fact]
    public void AgregarTelegram_TokenConFormatoInvalido_NoIncluyeElTokenEnElMensaje()
    {
        const string tokenMalo = "formato:invalido:con-secreto";

        var excepcion = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AgregarTelegram(tokenMalo, chatAutorizado: 555));

        Assert.DoesNotContain(tokenMalo, excepcion.Message, StringComparison.Ordinal);
    }

    // El adaptador guarda el offset entre llamadas: si no fuera singleton, cada corrida
    // arrancaría en 0 y Telegram volvería a entregar lo ya leído.
    [Fact]
    public void AgregarTelegram_RegistraLaFuenteComoSingleton()
    {
        var servicios = new ServiceCollection().AgregarTelegram(Token, chatAutorizado: 555);

        var descriptor = Assert.Single(servicios, s => s.ServiceType == typeof(IFuenteMensajes));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    // El caso de uso se registra en esta extensión porque el chat autorizado —su única
    // dependencia primitiva— entra por acá. El composition root sólo llama extensiones.
    [Fact]
    public void AgregarTelegram_RegistraElCasoDeUsoDeIngesta()
    {
        var servicios = new ServiceCollection().AgregarTelegram(Token, chatAutorizado: 555);

        Assert.Contains(servicios, s => s.ServiceType == typeof(IngestarMensajes));
    }
}
