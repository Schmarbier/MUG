using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.CasosDeUso;

/// <summary>
/// Lee la fuente de mensajes, descarta lo que no viene del chat autorizado, deduplica por
/// message_id y guarda el resto sin procesar (FR-01 a FR-04). Vive en Domain: no sabe que la
/// fuente es Telegram ni que la persistencia es SQLite.
/// </summary>
public sealed class IngestarMensajes
{
    /// <summary>
    /// M-04 (threat model): tope de mensajes por corrida. El resto queda en la fuente para la
    /// próxima (los updates de Telegram viven 24 h). Sin el tope, una tanda grande dispara N
    /// llamadas al modelo de hasta 15 s cada una y la corrida se vuelve interminable.
    /// </summary>
    public const int MaximoPorCorrida = 100;

    private readonly IFuenteMensajes _fuente;
    private readonly IRepositorioMensajes _repositorio;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReloj _reloj;
    private readonly long _chatAutorizado;

    public IngestarMensajes(
        IFuenteMensajes fuente,
        IRepositorioMensajes repositorio,
        IUnitOfWork unitOfWork,
        IReloj reloj,
        long chatAutorizado)
    {
        ArgumentNullException.ThrowIfNull(fuente);
        ArgumentNullException.ThrowIfNull(repositorio);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(reloj);

        _fuente = fuente;
        _repositorio = repositorio;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
        _chatAutorizado = chatAutorizado;
    }

    public async Task<Resultado> EjecutarAsync(CancellationToken cancellationToken)
    {
        // Con el chat autorizado en 0 (placeholder de configuración) el bot no ingiere nada, y
        // ni siquiera molesta a la fuente.
        if (_chatAutorizado == 0)
        {
            return new Resultado(Leidos: 0, Guardados: 0);
        }

        var entrantes = await _fuente.LeerAsync(MaximoPorCorrida, cancellationToken);

        var guardados = 0;
        var aceptadosEnEstaCorrida = new HashSet<long>();

        foreach (var entrante in entrantes.Take(MaximoPorCorrida))
        {
            if (!EsAceptable(entrante))
            {
                continue;
            }

            // La fuente es input no confiable: si repitiera un message_id dentro de la misma
            // tanda, ExisteAsync no lo vería (todavía no se confirmó) y el índice único
            // rompería la corrida entera al confirmar.
            if (!aceptadosEnEstaCorrida.Add(entrante.MessageId))
            {
                continue;
            }

            if (await _repositorio.ExisteAsync(entrante.MessageId, cancellationToken))
            {
                continue;
            }

            var mensaje = new Mensaje(
                entrante.MessageId,
                Truncar(entrante.Texto),
                _reloj.UtcNow);

            await _repositorio.AgregarAsync(mensaje, cancellationToken);
            guardados++;
        }

        if (guardados > 0)
        {
            await _unitOfWork.ConfirmarAsync(cancellationToken);
        }

        return new Resultado(entrantes.Count, guardados);
    }

    private bool EsAceptable(MensajeEntrante entrante) =>
        entrante.ChatId == _chatAutorizado &&      // FR-02
        entrante.MessageId > 0 &&
        !string.IsNullOrWhiteSpace(entrante.Texto);

    private static string Truncar(string texto) =>
        texto.Length <= Mensaje.TextoMaximo ? texto : texto[..Mensaje.TextoMaximo];

    /// <summary>
    /// Resumen de la corrida. Son cantidades: M-03 prohíbe que el log lleve el texto de los
    /// mensajes.
    /// </summary>
    public sealed record Resultado(int Leidos, int Guardados);
}
