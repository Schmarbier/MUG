using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using PersonalFinance.Domain.CasosDeUso;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Ollama;

/// <summary>
/// Registro del clasificador. Recibe primitivos, nunca el objeto de configuración de la app.
/// </summary>
public static class AgregarClasificadorExtensions
{
    /// <param name="permitirOllamaRemoto">
    /// Opt-in explícito para apuntar a un Ollama que no está en la máquina. Exige además que la
    /// URI sea <c>https</c>.
    /// </param>
    public static IServiceCollection AgregarClasificador(
        this IServiceCollection servicios,
        Uri uri,
        string modelo,
        bool permitirOllamaRemoto = false)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelo);

        ValidarEndpoint(uri, permitirOllamaRemoto);

        var opciones = new OpcionesOllama(uri, modelo);
        servicios.AddSingleton(opciones);

        servicios.AddSingleton<IOllamaApiClient>(_ => new OllamaApiClient(
            new HttpClient { BaseAddress = uri, Timeout = opciones.Timeout },
            modelo));

        servicios.AddScoped<IClasificador, ClasificadorOllama>();

        // Mismo criterio que AgregarTelegram con la ingesta: la extensión que habilita el
        // adaptador registra el caso de uso que lo consume, así el composition root sólo llama
        // extensiones y no registra servicios sueltos.
        servicios.AddScoped<ClasificarMensajesPendientes>();

        return servicios;
    }

    /// <summary>
    /// M-02 (threat model): el texto de los mensajes es PII financiera y viaja en HTTP plano.
    /// Mientras no salga de la máquina eso es aceptable; apuntando a un host remoto deja de
    /// serlo, y contra un endpoint que además no tiene autenticación. Por eso salir de loopback
    /// requiere opt-in explícito y, encima, <c>https</c>.
    /// </summary>
    private static void ValidarEndpoint(Uri uri, bool permitirOllamaRemoto)
    {
        if (uri.IsLoopback)
        {
            return;
        }

        if (!permitirOllamaRemoto)
        {
            throw new ArgumentException(
                $"El endpoint de Ollama ({uri.Host}) no es loopback. El texto de los mensajes es " +
                "información financiera y viaja sin cifrar: para salir de la máquina hay que " +
                "activar PermitirOllamaRemoto a propósito.",
                nameof(uri));
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"El endpoint remoto de Ollama ({uri.Host}) tiene que ser https: fuera de la " +
                "máquina, el texto de los mensajes no puede viajar en claro.",
                nameof(uri));
        }
    }
}
