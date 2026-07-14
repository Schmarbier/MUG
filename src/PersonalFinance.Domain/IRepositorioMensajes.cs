namespace PersonalFinance.Domain;

// Puerto de persistencia de mensajes. La implementación real (EF/SQLite) vive en Infrastructure;
// el núcleo solo conoce esta abstracción, lo que permite testear la lógica sin base de datos.
public interface IRepositorioMensajes
{
    Task<bool> ExisteAsync(long messageId, CancellationToken ct = default);
    Task AgregarAsync(Mensaje mensaje, CancellationToken ct = default);

    // Para el procesamiento: mensajes pendientes de clasificar y cierre de la unidad de trabajo.
    Task<IReadOnlyList<Mensaje>> ObtenerNoProcesadosAsync(CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
