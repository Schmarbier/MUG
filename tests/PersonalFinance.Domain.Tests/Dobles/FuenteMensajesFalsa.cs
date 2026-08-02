using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Dobles;

/// <summary>
/// Doble de <see cref="IFuenteMensajes"/>. Se mockean puertos, nunca entidades: así el caso de
/// uso se prueba sin red y sin Telegram.
/// </summary>
internal sealed class FuenteMensajesFalsa : IFuenteMensajes
{
    private readonly IReadOnlyList<MensajeEntrante> _entrantes;
    private readonly Exception? _falla;

    private FuenteMensajesFalsa(IReadOnlyList<MensajeEntrante> entrantes, Exception? falla)
    {
        _entrantes = entrantes;
        _falla = falla;
    }

    public int Llamadas { get; private set; }

    public int? UltimoMaximoPedido { get; private set; }

    public static FuenteMensajesFalsa Con(params MensajeEntrante[] entrantes) => new(entrantes, falla: null);

    public static FuenteMensajesFalsa QueFalla(Exception falla) => new([], falla);

    public Task<IReadOnlyList<MensajeEntrante>> LeerAsync(int maximo, CancellationToken cancellationToken)
    {
        Llamadas++;
        UltimoMaximoPedido = maximo;

        return _falla is not null
            ? Task.FromException<IReadOnlyList<MensajeEntrante>>(_falla)
            : Task.FromResult(_entrantes);
    }
}
