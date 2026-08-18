using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Puertos;

/// <summary>
/// Puerto de persistencia de mensajes. El dominio declara qué necesita; la infraestructura
/// decide cómo.
/// </summary>
public interface IRepositorioMensajes
{
    /// <summary>Resuelve la deduplicación por <c>message_id</c> de Telegram (FR-04).</summary>
    Task<bool> ExisteAsync(long messageId, CancellationToken cancellationToken);

    Task AgregarAsync(Mensaje mensaje, CancellationToken cancellationToken);

    /// <summary>Mensajes con <c>procesado = false</c> y <c>error = false</c> (FR-06).</summary>
    Task<IReadOnlyList<Mensaje>> ObtenerPendientesAsync(CancellationToken cancellationToken);
}
