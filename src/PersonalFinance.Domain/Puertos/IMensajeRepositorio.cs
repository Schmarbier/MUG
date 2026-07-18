using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Puertos;

public interface IMensajeRepositorio
{
    Task<bool> ExisteConIdentificadorCanalAsync(long identificadorCanal, CancellationToken ct = default);
    Task<Mensaje?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Mensaje>> ListarPendientesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Mensaje>> ListarConErrorAsync(CancellationToken ct = default);
    Task AgregarAsync(Mensaje mensaje, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
