using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using PersonalFinance.Bot.Data;
using PersonalFinance.Bot.Services;

namespace PersonalFinance.Bot.Telegram;

/// <summary>
/// Escucha mensajes de Telegram por long-polling (RF-01). Por cada mensaje de texto lo
/// ingiere (guardado + deduplicación) y dispara el procesador. No responde al usuario
/// (fuera de alcance): solo loguea por consola.
/// </summary>
public sealed class TelegramListener
{
    private readonly ITelegramBotClient _bot;
    private readonly Func<PersonalFinanceContext> _dbFactory;
    private readonly MensajeParser _parser;
    private readonly IClasificador _clasificador;

    public TelegramListener(
        ITelegramBotClient bot,
        Func<PersonalFinanceContext> dbFactory,
        MensajeParser parser,
        IClasificador clasificador)
    {
        _bot = bot;
        _dbFactory = dbFactory;
        _parser = parser;
        _clasificador = clasificador;
    }

    public void Start(CancellationToken ct)
    {
        var opciones = new ReceiverOptions { AllowedUpdates = [UpdateType.Message] };
        _bot.StartReceiving(OnUpdate, OnError, opciones, ct);
    }

    private async Task OnUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: { Length: > 0 } texto } msg)
            return;

        var fecha = new DateTimeOffset(DateTime.SpecifyKind(msg.Date, DateTimeKind.Utc));

        await using var db = _dbFactory();

        var resultado = await new IngestaService(db).GuardarAsync(msg.MessageId, texto, fecha, ct);
        Console.WriteLine($"[ingesta] msg {msg.MessageId} -> {resultado}: \"{texto}\"");

        var creados = await new ProcesadorService(db, _parser, _clasificador).ProcesarPendientesAsync(ct);
        if (creados > 0)
            Console.WriteLine($"[procesador] {creados} movimiento(s) creado(s).");
    }

    private Task OnError(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.Error.WriteLine($"[telegram] error: {ex.Message}");
        return Task.CompletedTask;
    }
}
