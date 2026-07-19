using PersonalFinance.Domain.Puertos;
using PersonalFinance.Domain.Servicios;

namespace PersonalFinance.Bot;

/// <summary>
/// Barrido periódico de mensajes pendientes, existan o no mensajes nuevos (R4). Sin esto,
/// FR-010a sería incumplible en el escenario más probable: el clasificador caído sin
/// tráfico nuevo nunca alcanzaría los 3 intentos que FR-010b necesita.
/// </summary>
public class BarridoClasificacionBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<BarridoClasificacionBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(Intervalo);

        do
        {
            await ProcesarPendientesAsync(stoppingToken);
        } while (await temporizador.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcesarPendientesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mensajeRepositorio = scope.ServiceProvider.GetRequiredService<IMensajeRepositorio>();
        var clasificacionServicio = scope.ServiceProvider.GetRequiredService<ClasificacionServicio>();

        var pendientes = await mensajeRepositorio.ListarPendientesAsync(ct);

        if (pendientes.Count == 0)
        {
            return;
        }

        logger.LogInformation("[barrido] {Cantidad} mensajes pendientes", pendientes.Count);

        foreach (var mensaje in pendientes)
        {
            await clasificacionServicio.ClasificarAsync(mensaje, ct);

            if (mensaje.Procesado)
            {
                logger.LogInformation("[barrido] Mensaje {Id} procesado correctamente", mensaje.Id);
            }
            else if (mensaje.TieneError)
            {
                logger.LogWarning("[barrido] Mensaje {Id} con error: {Motivo}", mensaje.Id, mensaje.MotivoError);
            }
            else
            {
                logger.LogInformation(
                    "[barrido] Mensaje {Id} sigue pendiente, intento {Intentos}/{Maximo}",
                    mensaje.Id, mensaje.IntentosClasificacion, ClasificacionServicio.MaximoIntentos);
            }
        }
    }
}
