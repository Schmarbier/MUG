namespace PersonalFinance.Domain;

// Núcleo de la ingesta (RF-01 a RF-04): filtra por chat autorizado, deduplica por
// message_id y persiste el mensaje nuevo como "no procesado".
public class ServicioIngesta
{
    private readonly IRepositorioMensajes _repo;
    private readonly long _chatAutorizado;

    public ServicioIngesta(IRepositorioMensajes repo, long chatAutorizado)
    {
        _repo = repo;
        _chatAutorizado = chatAutorizado;
    }

    public async Task<ResultadoIngesta> IngerirAsync(MensajeEntrante entrante, CancellationToken ct = default)
    {
        if (entrante.ChatId != _chatAutorizado)
            return ResultadoIngesta.Descartado;

        if (await _repo.ExisteAsync(entrante.MessageId, ct))
            return ResultadoIngesta.Duplicado;

        var mensaje = new Mensaje
        {
            MessageId = entrante.MessageId,
            ChatId = entrante.ChatId,
            Texto = entrante.Texto,
            FechaMensaje = entrante.Fecha,
            Procesado = false,
            Error = false,
        };

        await _repo.AgregarAsync(mensaje, ct);
        return ResultadoIngesta.Guardado;
    }
}
