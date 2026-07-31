namespace PersonalFinance.Domain.Puertos;

/// <summary>
/// Puerto de lectura del canal de mensajes (FR-01). El dominio no sabe que del otro lado hay
/// Telegram.
/// </summary>
public interface IFuenteMensajes
{
    /// <summary>
    /// Devuelve hasta <paramref name="maximo"/> mensajes de texto pendientes de leer. Lo que no
    /// entra queda en la fuente para la próxima corrida (M-04).
    /// </summary>
    Task<IReadOnlyList<MensajeEntrante>> LeerAsync(int maximo, CancellationToken cancellationToken);
}
