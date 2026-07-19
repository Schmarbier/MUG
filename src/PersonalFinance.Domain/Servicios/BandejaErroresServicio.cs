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
    ///
    /// Garantía (FR-017b): un mensaje que no se pudo resolver vuelve a quedar en error
    /// <b>persistido</b>, no solo en memoria, así sigue visible en la bandeja aunque la falla
    /// haya ocurrido después de que la limpieza del error ya se hubiera volcado a la base.
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
                // La cancelación sí corta el lote, pero antes deja este mensaje consistente:
                // si se canceló entre el guardado del movimiento y el del mensaje, la base ya
                // tiene TieneError = false y sin restaurar quedaría invisible.
                await RestaurarErrorAsync(mensaje, motivoPrevio);
                throw;
            }
            catch (Exception)
            {
                // Un mensaje que explota no puede abortar el lote: queda contado como no resuelto.
                await RestaurarErrorAsync(mensaje, motivoPrevio);
            }
        }

        return new ResultadoReprocesoMasivo(enError.Count, exitosos);
    }

    /// <summary>
    /// Devuelve el mensaje al estado de error que tenía antes del intento y lo PERSISTE.
    /// ReprocesarAsync ya le había limpiado el error; si esa limpieza alcanzó a llegar a la base
    /// (el guardado del movimiento vuelca el mismo DbContext) y solo la restauramos en memoria,
    /// el mensaje quedaría con TieneError = false y Procesado = false: ni en la bandeja de
    /// errores ni en ninguna otra pantalla. Eso es exactamente lo que FR-017b prohíbe.
    /// </summary>
    private async Task RestaurarErrorAsync(Mensaje mensaje, string? motivoPrevio)
    {
        mensaje.TieneError = true;
        mensaje.MotivoError = motivoPrevio;

        try
        {
            // CancellationToken.None a propósito: la cancelación puede ser justamente la causa
            // de la excepción que nos trajo hasta acá, y guardar con un token ya cancelado
            // fallaría dejando el mensaje invisible en el único caso que queremos cubrir.
            // Es una escritura mínima y acotada sobre un mensaje ya cargado: vale completarla.
            await mensajeRepositorio.GuardarCambiosAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // Si ni la restauración se puede guardar, no tumbamos el lote ni tapamos la excepción
            // original: el mensaje ya está contado como no resuelto y el resto de la bandeja
            // todavía puede reprocesarse. La base queda como la dejó la falla original, que es
            // lo mejor disponible cuando la persistencia entera está caída.
        }
    }
}
