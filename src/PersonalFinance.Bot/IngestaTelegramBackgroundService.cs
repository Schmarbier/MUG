using PersonalFinance.Domain.Servicios;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PersonalFinance.Bot;

/// <summary>
/// Long polling de Telegram.Bot. Guarda cada mensaje del chat autorizado y dispara la
/// clasificación en el mismo ciclo (FR-005a) — no espera al barrido periódico.
/// El bot NUNCA responde por Telegram (FR-037): esto solo lee.
/// </summary>
public class IngestaTelegramBackgroundService(
    ITelegramBotClient bot,
    IServiceScopeFactory scopeFactory,
    ILogger<IngestaTelegramBackgroundService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opciones = new ReceiverOptions { AllowedUpdates = [UpdateType.Message] };
        return bot.ReceiveAsync(ManejarUpdateAsync, ManejarErrorAsync, opciones, stoppingToken);
    }

    private async Task ManejarUpdateAsync(ITelegramBotClient _, Update update, CancellationToken ct)
    {
        var mensajeTelegram = update.Message;
        if (mensajeTelegram?.Text is null)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var ingestaServicio = scope.ServiceProvider.GetRequiredService<IngestaServicio>();
        var clasificacionServicio = scope.ServiceProvider.GetRequiredService<ClasificacionServicio>();

        var mensaje = await ingestaServicio.IngerirAsync(
            mensajeTelegram.Chat.Id,
            mensajeTelegram.MessageId,
            mensajeTelegram.Text,
            new DateTimeOffset(mensajeTelegram.Date, TimeSpan.Zero),
            ct);

        if (mensaje is null)
        {
            return;
        }

        logger.LogInformation("[ingesta] Mensaje guardado: {Id}", mensaje.Id);
        await clasificacionServicio.ClasificarAsync(mensaje, ct);
    }

    private Task ManejarErrorAsync(ITelegramBotClient _, Exception excepcion, CancellationToken ct)
    {
        logger.LogError(excepcion, "[ingesta] Error en el polling de Telegram");
        return Task.CompletedTask;
    }
}
