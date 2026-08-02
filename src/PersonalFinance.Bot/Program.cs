using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonalFinance.Domain.CasosDeUso;
using PersonalFinance.Infrastructure;
using PersonalFinance.Infrastructure.Ollama;
using PersonalFinance.Infrastructure.Persistencia;
using PersonalFinance.Infrastructure.Telegram;

// Composition root del bot: lee configuración, arma la DI llamando sólo a las tres extensiones
// y ejecuta seed -> ingesta -> clasificación. No tiene lógica de negocio.

// El content root es el directorio de salida, no el del terminal. `dotnet run --project X` usa
// como working directory el directorio desde donde se lo invoca, así que con el default
// appsettings.json no se encuentra y toda su configuración se pierde en silencio. Es el mismo
// motivo por el que la ruta de la base es absoluta.
var constructor = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// User secrets también fuera de Development: es donde vive el token en la máquina del dueño y
// este proceso se levanta a mano, sin variable de entorno de ambiente.
constructor.Configuration.AddUserSecrets<Program>(optional: true);

var configuracion = constructor.Configuration;
var token = configuracion["TelegramBotToken"] ?? string.Empty;
var chatAutorizado = configuracion.GetValue<long>("TelegramChatAutorizado");
var uriOllama = new Uri(configuracion["OllamaUri"] ?? OpcionesOllama.UriPorDefecto.ToString());
var modelo = configuracion["OLLAMA_MODEL"]
    ?? configuracion["OllamaModelo"]
    ?? OpcionesOllama.ModeloPorDefecto;
var permitirOllamaRemoto = configuracion.GetValue<bool>("PermitirOllamaRemoto");

constructor.Services
    // Sin CadenaConexion configurada usa %LOCALAPPDATA%\PersonalFinance\personalfinance.db, la
    // ruta absoluta y estable que comparten Bot y Web. El override existe para poder correr el
    // proceso contra una base descartable sin tocar la real.
    .AgregarPersistencia(configuracion["CadenaConexion"])
    .AgregarTelegram(token, chatAutorizado)
    .AgregarClasificador(uriOllama, modelo, permitirOllamaRemoto);

using var host = constructor.Build();

using var cancelacion = new CancellationTokenSource();
Console.CancelKeyPress += (_, evento) =>
{
    evento.Cancel = true;
    cancelacion.Cancel();
};

var registro = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PersonalFinance.Bot");

using var alcance = host.Services.CreateScope();
var servicios = alcance.ServiceProvider;

await servicios.GetRequiredService<SeedCategorias>().EjecutarAsync(cancelacion.Token);

if (chatAutorizado == 0)
{
    registro.LogWarning(
        "TelegramChatAutorizado está en 0: el bot no va a ingerir ningún mensaje. " +
        "Cargá el id del chat del dueño con user-secrets o como variable de entorno.");
}

try
{
    var ingesta = await servicios.GetRequiredService<IngestarMensajes>().EjecutarAsync(cancelacion.Token);

    // M-03: el resumen informa cantidades, nunca el texto de los mensajes ni configuración.
    registro.LogInformation(
        "Mensajes guardados: {Guardados} (leídos: {Leidos})", ingesta.Guardados, ingesta.Leidos);
}
catch (Exception excepcion) when (excepcion is not OperationCanceledException)
{
    // La ingesta abortó sin guardar nada y los mensajes siguen en Telegram. La corrida no
    // termina acá a propósito: lo que se ingirió en corridas anteriores todavía está pendiente
    // de clasificar, y no hay razón para dejarlo esperando porque hoy falló la red.
    registro.LogError("No se pudo leer el canal de mensajes: {Detalle}", excepcion.Message);
}

var clasificacion = await servicios.GetRequiredService<ClasificarMensajesPendientes>()
    .EjecutarAsync(cancelacion.Token);

registro.LogInformation(
    "Clasificación: {Clasificados} movimientos, {ConError} con error, {NoDisponibles} sin clasificador",
    clasificacion.Clasificados,
    clasificacion.ConError,
    clasificacion.NoDisponibles);

if (clasificacion.Abortada)
{
    registro.LogWarning(
        "La corrida de clasificación se cortó antes de terminar. Los mensajes que quedaron " +
        "pendientes se retoman en la próxima corrida.");
}
