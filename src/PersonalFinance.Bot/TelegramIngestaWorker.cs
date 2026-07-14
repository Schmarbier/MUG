using PersonalFinance.Domain;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PersonalFinance.Bot;

// Adaptador de Telegram: recibe updates por long polling y delega la lógica al ServicioIngesta.
public class TelegramIngestaWorker : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramIngestaWorker> _logger;

    public TelegramIngestaWorker(
        ITelegramBotClient bot, IServiceScopeFactory scopeFactory, ILogger<TelegramIngestaWorker> logger)
    {
        _bot = bot;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opciones = new ReceiverOptions { AllowedUpdates = [UpdateType.Message] };
        _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, opciones, stoppingToken);

        var yo = await _bot.GetMe(stoppingToken);
        _logger.LogInformation("[ingesta] Bot @{Username} escuchando mensajes", yo.Username);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Apagado normal del host.
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: { Length: > 0 } texto } mensaje)
            return;

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ServicioIngesta>();

        var entrante = new MensajeEntrante(mensaje.MessageId, mensaje.Chat.Id, texto, mensaje.Date);
        var resultado = await servicio.IngerirAsync(entrante, ct);

        _logger.LogInformation("[ingesta] msg {MessageId} chat {ChatId}: {Resultado}",
            mensaje.MessageId, mensaje.Chat.Id, resultado);

        // Recién ingerido => clasificarlo (y cualquier otro pendiente) contra el modelo.
        if (resultado == ResultadoIngesta.Guardado)
        {
            var procesamiento = scope.ServiceProvider.GetRequiredService<ServicioProcesamiento>();
            var procesados = await procesamiento.ProcesarPendientesAsync(ct);
            _logger.LogInformation("[procesador] {Procesados} mensaje(s) pendiente(s) procesado(s)", procesados);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "[ingesta] error de polling");
        return Task.CompletedTask;
    }
}
