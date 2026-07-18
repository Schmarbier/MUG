using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Servicios;

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
}
