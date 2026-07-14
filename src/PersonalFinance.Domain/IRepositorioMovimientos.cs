namespace PersonalFinance.Domain;

public interface IRepositorioMovimientos
{
    // Agrega al contexto sin persistir; el guardado lo cierra GuardarCambiosAsync
    // (unit of work) para que movimiento + estado del mensaje se persistan juntos.
    Task AgregarAsync(Movimiento movimiento, CancellationToken ct = default);
}
