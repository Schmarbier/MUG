using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Infrastructure.Ollama;
using Xunit;

namespace PersonalFinance.Infrastructure.Tests;

public class AgregarClasificadorExtensionsTests
{
    // Valida M-02: el texto de los mensajes es información financiera y viaja sin cifrar.
    // Mientras no salga de la máquina es aceptable; apuntando a otro host deja de serlo, así que
    // el arranque falla salvo opt-in explícito.
    [Theory]
    [InlineData("http://192.168.0.50:11434")]
    [InlineData("http://0.0.0.0:11434")]
    [InlineData("http://ollama.interno.lan:11434")]
    public void AgregarClasificador_UriNoLoopbackSinOptIn_FallaAlArrancar(string uri)
    {
        var excepcion = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AgregarClasificador(new Uri(uri), OpcionesOllama.ModeloPorDefecto));

        Assert.Equal("uri", excepcion.ParamName);
    }

    // Sad path de M-02: el opt-in no alcanza. Fuera de la máquina, en claro, no viaja.
    [Fact]
    public void AgregarClasificador_UriNoLoopbackConOptInYHttp_FallaPorNoSerHttps()
    {
        var excepcion = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AgregarClasificador(
                new Uri("http://192.168.0.50:11434"),
                OpcionesOllama.ModeloPorDefecto,
                permitirOllamaRemoto: true));

        Assert.Contains("https", excepcion.Message, StringComparison.Ordinal);
    }

    // El opt-in bien usado sí registra: la mitigación no rompe el caso legítimo.
    [Fact]
    public void AgregarClasificador_UriRemotaConOptInYHttps_RegistraElClasificador()
    {
        var servicios = new ServiceCollection().AgregarClasificador(
            new Uri("https://ollama.interno.lan:11434"),
            OpcionesOllama.ModeloPorDefecto,
            permitirOllamaRemoto: true);

        Assert.Contains(servicios, s => s.ServiceType == typeof(IClasificador));
    }

    // El caso normal: loopback, sin opt-in, sin fricción.
    [Fact]
    public void AgregarClasificador_UriLoopback_RegistraElClasificador()
    {
        var servicios = new ServiceCollection().AgregarClasificador(
            OpcionesOllama.UriPorDefecto,
            OpcionesOllama.ModeloPorDefecto);

        Assert.Contains(servicios, s => s.ServiceType == typeof(IClasificador));
    }
}
