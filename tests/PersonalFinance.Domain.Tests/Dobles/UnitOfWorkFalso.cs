using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Dobles;

/// <summary>
/// Doble de <see cref="IUnitOfWork"/>. Cuenta confirmaciones y, si se le pide, falla: es como se
/// prueba que un fallo al persistir no deja el mensaje marcado.
/// </summary>
internal sealed class UnitOfWorkFalso : IUnitOfWork
{
    public int Confirmaciones { get; private set; }

    public Exception? FallaAlConfirmar { get; set; }

    public Task ConfirmarAsync(CancellationToken cancellationToken)
    {
        Confirmaciones++;

        return FallaAlConfirmar is not null
            ? Task.FromException(FallaAlConfirmar)
            : Task.CompletedTask;
    }
}
