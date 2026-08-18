namespace PersonalFinance.Domain.Puertos;

/// <summary>
/// Puerto de confirmación atómica. La clasificación exige que el movimiento y el nuevo estado del
/// mensaje se guarden juntos o no se guarde ninguno; esa garantía es una necesidad del dominio y
/// por eso se declara acá, en vez de quedar librada a que los repositorios compartan
/// implementación.
/// </summary>
public interface IUnitOfWork
{
    Task ConfirmarAsync(CancellationToken cancellationToken);
}
