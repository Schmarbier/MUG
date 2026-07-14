using PersonalFinance.Domain;

namespace PersonalFinance.Domain.Tests;

public class ServicioIngestaTests
{
    private const long ChatDuenio = 100;

    // Fake del puerto: sin DB, sin Telegram. El test ejerce solo la lógica de ingesta.
    private sealed class RepositorioFake : IRepositorioMensajes
    {
        public List<Mensaje> Guardados { get; } = new();

        public Task<bool> ExisteAsync(long messageId, CancellationToken ct = default)
            => Task.FromResult(Guardados.Exists(m => m.MessageId == messageId));

        public Task AgregarAsync(Mensaje mensaje, CancellationToken ct = default)
        {
            Guardados.Add(mensaje);
            return Task.CompletedTask;
        }

        // No usados por la ingesta; el puerto los expone para el procesamiento.
        public Task<IReadOnlyList<Mensaje>> ObtenerNoProcesadosAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Mensaje>>(Guardados);
        public Task GuardarCambiosAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    [Fact] // RF-02 / AC-02
    public async Task Mensaje_de_chat_no_autorizado_se_descarta_sin_guardar()
    {
        var repo = new RepositorioFake();
        var servicio = new ServicioIngesta(repo, ChatDuenio);

        var resultado = await servicio.IngerirAsync(
            new MensajeEntrante(MessageId: 1, ChatId: 999, "hola", DateTime.UtcNow));

        Assert.Equal(ResultadoIngesta.Descartado, resultado);
        Assert.Empty(repo.Guardados);
    }

    [Fact] // RF-01, RF-03 / AC-01
    public async Task Mensaje_del_chat_autorizado_se_guarda_como_no_procesado()
    {
        var repo = new RepositorioFake();
        var servicio = new ServicioIngesta(repo, ChatDuenio);

        var resultado = await servicio.IngerirAsync(
            new MensajeEntrante(MessageId: 1, ChatDuenio, "$2000 super", DateTime.UtcNow));

        Assert.Equal(ResultadoIngesta.Guardado, resultado);
        var mensaje = Assert.Single(repo.Guardados);
        Assert.False(mensaje.Procesado);
        Assert.False(mensaje.Error);
        Assert.Equal("$2000 super", mensaje.Texto);
    }

    [Fact] // RF-04 / AC-03
    public async Task Mensaje_con_message_id_repetido_no_se_guarda_dos_veces()
    {
        var repo = new RepositorioFake();
        var servicio = new ServicioIngesta(repo, ChatDuenio);
        await servicio.IngerirAsync(new MensajeEntrante(123, ChatDuenio, "primero", DateTime.UtcNow));

        var resultado = await servicio.IngerirAsync(
            new MensajeEntrante(123, ChatDuenio, "otra vez", DateTime.UtcNow));

        Assert.Equal(ResultadoIngesta.Duplicado, resultado);
        Assert.Single(repo.Guardados);
    }
}
