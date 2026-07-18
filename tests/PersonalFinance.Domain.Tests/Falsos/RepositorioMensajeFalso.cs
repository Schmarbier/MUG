using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Falsos;

public sealed class RepositorioMensajeFalso : IMensajeRepositorio
{
    public List<Mensaje> Mensajes { get; } = [];

    public Task<bool> ExisteConIdentificadorCanalAsync(long identificadorCanal, CancellationToken ct = default) =>
        Task.FromResult(Mensajes.Any(m => m.IdentificadorCanal == identificadorCanal));

    public Task<Mensaje?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Mensajes.FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<Mensaje>> ListarPendientesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Mensaje>>(Mensajes.Where(m => !m.Procesado && !m.TieneError).ToList());

    public Task<IReadOnlyList<Mensaje>> ListarConErrorAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Mensaje>>(Mensajes.Where(m => m.TieneError).ToList());

    public Task AgregarAsync(Mensaje mensaje, CancellationToken ct = default)
    {
        mensaje.Id = Mensajes.Count == 0 ? 1 : Mensajes.Max(m => m.Id) + 1;
        Mensajes.Add(mensaje);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => Task.CompletedTask;
}
