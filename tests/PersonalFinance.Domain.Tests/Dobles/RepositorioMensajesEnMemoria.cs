using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Dobles;

/// <summary>
/// Doble de <see cref="IRepositorioMensajes"/>.
/// <see cref="YaGuardados"/> representa lo que ya está confirmado en la base; <see cref="Agregados"/>,
/// lo que el caso de uso agregó en esta corrida y todavía no confirmó. La distinción importa:
/// es la que hace visible si la deduplicación se apoya sólo en la base.
/// </summary>
internal sealed class RepositorioMensajesEnMemoria : IRepositorioMensajes
{
    public HashSet<long> YaGuardados { get; } = [];

    public List<Mensaje> Agregados { get; } = [];

    public List<Mensaje> Pendientes { get; } = [];

    public Task<bool> ExisteAsync(long messageId, CancellationToken cancellationToken) =>
        Task.FromResult(YaGuardados.Contains(messageId));

    public Task AgregarAsync(Mensaje mensaje, CancellationToken cancellationToken)
    {
        Agregados.Add(mensaje);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Mensaje>> ObtenerPendientesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Mensaje>>(Pendientes);
}
