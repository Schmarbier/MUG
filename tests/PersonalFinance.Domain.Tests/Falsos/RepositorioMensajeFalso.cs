using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Falsos;

public sealed class RepositorioMensajeFalso : IMensajeRepositorio
{
    public List<Mensaje> Mensajes { get; } = [];

    /// <summary>
    /// Estado "en la base" de cada mensaje, separado del estado en memoria. Sin esto el falso
    /// tapa los bugs de persistencia: como devuelve las mismas instancias, un cambio que nunca
    /// se guardó se vería igual que uno guardado. Mientras un mensaje no tenga snapshot se
    /// asume que lo que está en memoria es lo persistido (mensajes armados a mano en los tests).
    /// </summary>
    private readonly Dictionary<int, bool> _errorPersistido = [];

    /// <summary>
    /// Excepción a lanzar en el próximo <see cref="GuardarCambiosAsync"/>, para simular una falla
    /// de persistencia. Se consume una sola vez: el guardado siguiente vuelve a funcionar.
    /// </summary>
    public Exception? ErrorAlGuardar { get; set; }

    /// <summary>
    /// Vuelca el estado en memoria a la "base". Es público porque el DbContext real es compartido:
    /// un SaveChanges de otro repositorio también persiste los cambios pendientes del mensaje.
    /// </summary>
    public void ConfirmarPersistencia()
    {
        foreach (var mensaje in Mensajes)
        {
            _errorPersistido[mensaje.Id] = mensaje.TieneError;
        }
    }

    public Task<bool> ExisteConIdentificadorCanalAsync(long identificadorCanal, CancellationToken ct = default) =>
        Task.FromResult(Mensajes.Any(m => m.IdentificadorCanal == identificadorCanal));

    public Task<Mensaje?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Mensajes.FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<Mensaje>> ListarPendientesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Mensaje>>(Mensajes.Where(m => !m.Procesado && !m.TieneError).ToList());

    // Responde por el estado PERSISTIDO: es la consulta que respalda la bandeja de errores
    // (FR-017b), y lo que importa es qué ve una pantalla que vuelve a leer de la base.
    public Task<IReadOnlyList<Mensaje>> ListarConErrorAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Mensaje>>(Mensajes.Where(TieneErrorEnLaBase).ToList());

    private bool TieneErrorEnLaBase(Mensaje mensaje) =>
        _errorPersistido.TryGetValue(mensaje.Id, out var persistido) ? persistido : mensaje.TieneError;

    public Task AgregarAsync(Mensaje mensaje, CancellationToken ct = default)
    {
        mensaje.Id = Mensajes.Count == 0 ? 1 : Mensajes.Max(m => m.Id) + 1;
        Mensajes.Add(mensaje);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default)
    {
        if (ErrorAlGuardar is not null)
        {
            var error = ErrorAlGuardar;
            ErrorAlGuardar = null;
            return Task.FromException(error);
        }

        ConfirmarPersistencia();
        return Task.CompletedTask;
    }
}
