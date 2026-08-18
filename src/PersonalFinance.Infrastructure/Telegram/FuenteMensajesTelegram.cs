using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PersonalFinance.Infrastructure.Telegram;

/// <summary>
/// Adaptador de <see cref="IFuenteMensajes"/> sobre Telegram. Es el único tipo del repo que
/// importa <c>Telegram.Bot</c>.
/// </summary>
public sealed class FuenteMensajesTelegram : IFuenteMensajes
{
    private readonly ITelegramBotClient _cliente;
    private readonly OpcionesTelegram _opciones;

    /// <summary>
    /// Offset de la API de updates. Es estado de instancia, no un <c>static</c>: AGENTS.md
    /// prohíbe los estáticos con estado. Se registra como singleton, así el ciclo de vida lo
    /// gestiona la DI y en un test se reemplaza sin arrastrar nada de la corrida anterior.
    /// </summary>
    private int _offset;

    public FuenteMensajesTelegram(ITelegramBotClient cliente, OpcionesTelegram opciones)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        ArgumentNullException.ThrowIfNull(opciones);

        _cliente = cliente;
        _opciones = opciones;
    }

    public async Task<IReadOnlyList<MensajeEntrante>> LeerAsync(int maximo, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximo, 1);

        var updates = await PedirUpdatesAsync(maximo, cancellationToken);

        var mensajes = new List<MensajeEntrante>(updates.Length);

        foreach (var update in updates)
        {
            // El offset avanza con TODOS los updates recibidos, no sólo con los que sirven: si
            // avanzara sólo con los mensajes de texto, una foto quedaría pendiente para siempre
            // y se releería en cada corrida.
            _offset = Math.Max(_offset, update.Id + 1);

            if (update.Message is not { Text: { } texto } mensaje || string.IsNullOrWhiteSpace(texto))
            {
                // Foto, sticker, audio o edición: se descarta sin guardar. No es un error.
                continue;
            }

            mensajes.Add(new MensajeEntrante(mensaje.Chat.Id, mensaje.MessageId, Truncar(texto)));
        }

        return mensajes;
    }

    private async Task<Update[]> PedirUpdatesAsync(int maximo, CancellationToken cancellationToken)
    {
        try
        {
            return await _cliente.GetUpdates(
                offset: _offset,
                limit: maximo,
                timeout: 0,
                allowedUpdates: [UpdateType.Message],
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException excepcion) when (excepcion.ErrorCode == 401)
        {
            // Token inválido: falla con mensaje explícito y no reintenta. Reintentar con un
            // token que Telegram ya rechazó no cambia el resultado.
            throw new InvalidOperationException(
                "Telegram rechazó el token del bot (401 Unauthorized). Revisá la clave " +
                "TelegramBotToken en user-secrets o en las variables de entorno.");
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            throw Sanitizada(excepcion);
        }
    }

    /// <summary>
    /// M-03 (threat model): Telegram.Bot incluye la URL de la request en el texto de sus
    /// excepciones, y esa URL lleva el token adentro. Se re-lanza sin el token y
    /// deliberadamente <b>sin excepción interna</b>: conservarla como inner volvería a filtrar
    /// el token apenas alguien loguee el <c>ToString()</c>. Se paga el stack trace original a
    /// cambio de que el secreto no llegue nunca a un log.
    /// </summary>
    private Exception Sanitizada(Exception excepcion)
    {
        var mensaje = excepcion.Message.Replace(_opciones.Token, "***", StringComparison.Ordinal);

        return new InvalidOperationException(
            $"Falló la lectura de updates de Telegram ({excepcion.GetType().Name}): {mensaje}");
    }

    private static string Truncar(string texto) =>
        texto.Length <= Mensaje.TextoMaximo ? texto : texto[..Mensaje.TextoMaximo];
}
