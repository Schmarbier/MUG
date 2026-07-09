using Microsoft.EntityFrameworkCore;
using PersonalFinance.Bot.Data;
using PersonalFinance.Bot.Domain;

namespace PersonalFinance.Bot.Services;

/// <summary>Resultado de intentar ingerir un mensaje de Telegram.</summary>
public enum ResultadoIngesta
{
    /// <summary>Mensaje nuevo: quedó guardado como "no procesado".</summary>
    Guardado,
    /// <summary>Ya existía un mensaje con ese MessageId: se ignoró (deduplicación).</summary>
    Duplicado
}

/// <summary>
/// Lee y guarda mensajes de Telegram (RF-01, RF-02). Deduplica por el MessageId de
/// Telegram para no re-guardar el mismo mensaje en cada corrida (riesgo del PRD).
/// </summary>
public sealed class IngestaService
{
    private readonly PersonalFinanceContext _db;

    public IngestaService(PersonalFinanceContext db) => _db = db;

    /// <summary>
    /// Guarda el mensaje si su <paramref name="messageId"/> no existía. El mensaje nace
    /// con estado "no procesado" (Procesado = false, Error = false).
    /// </summary>
    public async Task<ResultadoIngesta> GuardarAsync(
        long messageId, string texto, DateTimeOffset fechaRecibido, CancellationToken ct = default)
    {
        var yaExiste = await _db.Mensajes.AnyAsync(m => m.MessageId == messageId, ct);
        if (yaExiste)
            return ResultadoIngesta.Duplicado;

        _db.Mensajes.Add(new Mensaje
        {
            MessageId = messageId,
            Texto = texto,
            FechaRecibido = fechaRecibido
        });
        await _db.SaveChangesAsync(ct);

        return ResultadoIngesta.Guardado;
    }
}
