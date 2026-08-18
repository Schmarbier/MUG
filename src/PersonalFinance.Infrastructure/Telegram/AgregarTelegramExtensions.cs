using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Domain.CasosDeUso;
using PersonalFinance.Domain.Puertos;
using Telegram.Bot;

namespace PersonalFinance.Infrastructure.Telegram;

/// <summary>
/// Registro del canal de Telegram. Recibe primitivos, nunca el objeto de configuración de la
/// app: leer configuración es tarea exclusiva del composition root.
/// </summary>
public static partial class AgregarTelegramExtensions
{
    /// <summary>
    /// Valida el token y registra el cliente, la fuente de mensajes y el caso de uso que la
    /// consume. Es el punto donde el arranque falla si falta el secreto.
    /// </summary>
    /// <param name="chatAutorizado">
    /// Id del chat del dueño (FR-02). En <c>0</c> —el placeholder de <c>appsettings.json</c>—
    /// el bot arranca pero no ingiere nada.
    /// </param>
    public static IServiceCollection AgregarTelegram(
        this IServiceCollection servicios,
        string token,
        long chatAutorizado)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "El token del bot de Telegram es obligatorio. Cargalo en la clave " +
                "TelegramBotToken con user-secrets o como variable de entorno.",
                nameof(token));
        }

        if (!FormatoToken().IsMatch(token))
        {
            // No se incluye el valor recibido en el mensaje: M-03 prohíbe que el token llegue a
            // un log, y un mensaje de error termina en un log casi siempre.
            throw new ArgumentException(
                "El token del bot de Telegram no tiene el formato esperado (<id>:<secreto>).",
                nameof(token));
        }

        var opciones = new OpcionesTelegram(token, chatAutorizado);
        servicios.AddSingleton(opciones);

        servicios.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token));

        // Singleton porque el adaptador guarda el offset de updates entre llamadas.
        servicios.AddSingleton<IFuenteMensajes, FuenteMensajesTelegram>();

        // El caso de uso se registra acá y no en el composition root porque el chat autorizado
        // —su única dependencia primitiva— entra por esta extensión. AGENTS.md pide que el
        // composition root sólo llame extensiones, sin registrar servicios sueltos.
        servicios.AddScoped(proveedor => new IngestarMensajes(
            proveedor.GetRequiredService<IFuenteMensajes>(),
            proveedor.GetRequiredService<IRepositorioMensajes>(),
            proveedor.GetRequiredService<IUnitOfWork>(),
            proveedor.GetRequiredService<IReloj>(),
            chatAutorizado));

        return servicios;
    }

    [GeneratedRegex(@"^\d+:[A-Za-z0-9_-]+$")]
    private static partial Regex FormatoToken();
}
