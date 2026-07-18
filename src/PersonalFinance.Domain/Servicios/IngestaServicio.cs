using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Servicios;

/// <summary>
/// Filtra por el chat autorizado del dueño y deduplica por identificador de canal
/// (FR-002, FR-004). El índice único de Mensaje es la garantía real frente a la carrera
/// entre polling y barrido (R4); esta comprobación previa evita el viaje innecesario.
/// </summary>
public class IngestaServicio(IMensajeRepositorio mensajeRepositorio, long chatAutorizado)
{
    public async Task<Mensaje?> IngerirAsync(
        long chatId,
        long identificadorCanal,
        string texto,
        DateTimeOffset fechaRecepcionUtc,
        CancellationToken ct = default)
    {
        if (chatId != chatAutorizado)
        {
            return null;
        }

        if (await mensajeRepositorio.ExisteConIdentificadorCanalAsync(identificadorCanal, ct))
        {
            return null;
        }

        var mensaje = new Mensaje
        {
            IdentificadorCanal = identificadorCanal,
            Texto = texto,
            FechaRecepcionUtc = fechaRecepcionUtc,
            Procesado = false,
            IntentosClasificacion = 0,
            TieneError = false
        };

        await mensajeRepositorio.AgregarAsync(mensaje, ct);
        await mensajeRepositorio.GuardarCambiosAsync(ct);

        return mensaje;
    }
}
