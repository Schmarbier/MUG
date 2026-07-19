using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Servicios;

/// <summary>
/// Resultado de un reproceso masivo. <paramref name="Total"/> es la cantidad de mensajes que
/// estaban en error al arrancar; <paramref name="Exitosos"/> los que salieron del error;
/// <paramref name="ConError"/> los que siguen en error (por fallar de nuevo o por excepción).
/// </summary>
public readonly record struct ResultadoReprocesoMasivo(int Total, int Exitosos)
{
    public int ConError => Total - Exitosos;
}

/// <summary>Listado y reproceso de mensajes en error (US4).</summary>
public class BandejaErroresServicio(IMensajeRepositorio mensajeRepositorio, ClasificacionServicio clasificacionServicio)
{
    public Task<IReadOnlyList<Mensaje>> ListarAsync(CancellationToken ct = default) =>
        mensajeRepositorio.ListarConErrorAsync(ct);

    public async Task ReprocesarAsync(int mensajeId, CancellationToken ct = default)
    {
        var mensaje = await mensajeRepositorio.ObtenerPorIdAsync(mensajeId, ct)
            ?? throw new InvalidOperationException("El mensaje no existe.");

        // Solo se reprocesan mensajes en error; uno ya procesado no vuelve a generar movimientos.
        if (!mensaje.TieneError)
        {
            throw new InvalidOperationException("El mensaje no está en estado de error.");
        }

        mensaje.TieneError = false;
        mensaje.MotivoError = null;
        mensaje.IntentosClasificacion = 0;

        await clasificacionServicio.ClasificarAsync(mensaje, ct);
    }

    /// <summary>
    /// Reprocesa de una pasada todos los mensajes que están en error. Es resiliente a propósito:
    /// si uno falla (excepción del clasificador o de persistencia) se lo cuenta como no resuelto
    /// y se sigue con el resto, así un mensaje roto no bloquea el vaciado de la bandeja.
    /// Con la bandeja vacía devuelve un resultado en cero, sin lanzar.
    /// </summary>
    public async Task<ResultadoReprocesoMasivo> ReprocesarTodosAsync(CancellationToken ct = default)
    {
        var enError = await mensajeRepositorio.ListarConErrorAsync(ct);

        var exitosos = 0;
        foreach (var mensaje in enError)
        {
            var motivoPrevio = mensaje.MotivoError;

            try
            {
                await ReprocesarAsync(mensaje.Id, ct);

                if (!mensaje.TieneError)
                {
                    exitosos++;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Un mensaje que explota no puede abortar el lote: queda contado como no resuelto.
                // ReprocesarAsync ya le había limpiado el error, así que se lo restaura para que
                // no desaparezca de la bandeja por una falla que nunca llegó a resolverse.
                mensaje.TieneError = true;
                mensaje.MotivoError = motivoPrevio;
            }
        }

        return new ResultadoReprocesoMasivo(enError.Count, exitosos);
    }
}
