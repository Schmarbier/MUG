using PersonalFinance.Domain.CasosDeUso;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Domain.Tests.Dobles;
using Xunit;

namespace PersonalFinance.Domain.Tests;

public class IngestarMensajesTests
{
    private const long ChatAutorizado = 555;

    private readonly RepositorioMensajesEnMemoria _repositorio = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();

    private IngestarMensajes Crear(IFuenteMensajes fuente, long chatAutorizado = ChatAutorizado) =>
        new(fuente, _repositorio, _unitOfWork, new RelojFijo(), chatAutorizado);

    // Valida AC-01: el mensaje del chat autorizado se guarda sin procesar, sin error y con la
    // fecha de recepción que da el puerto del reloj (FR-03).
    [Fact]
    public async Task EjecutarAsync_MensajeNuevoDelChatAutorizado_LoGuardaNoProcesadoSinError()
    {
        var fuente = FuenteMensajesFalsa.Con(new MensajeEntrante(ChatAutorizado, 10, "$10.000 sueldo de julio"));

        await Crear(fuente).EjecutarAsync(CancellationToken.None);

        var mensaje = Assert.Single(_repositorio.Agregados);
        Assert.Equal(
            (10L, "$10.000 sueldo de julio", RelojFijo.Momento, false, false, (string?)null),
            (mensaje.MessageId, mensaje.Texto, mensaje.FechaRecepcion, mensaje.Procesado, mensaje.Error, mensaje.Motivo));
    }

    // Valida AC-02: un mensaje de otro chat no se guarda. Ni siquiera entra al sistema, así que
    // tampoco puede producir un movimiento.
    [Fact]
    public async Task EjecutarAsync_MensajeDeOtroChat_NoLoGuardaNiCreaMovimiento()
    {
        var fuente = FuenteMensajesFalsa.Con(new MensajeEntrante(ChatId: 999, MessageId: 10, Texto: "$5.000 nafta"));

        await Crear(fuente).EjecutarAsync(CancellationToken.None);

        Assert.Empty(_repositorio.Agregados);
        Assert.Equal(0, _unitOfWork.Confirmaciones);
    }

    // Valida AC-03: el mismo message_id no se guarda dos veces (FR-04).
    [Fact]
    public async Task EjecutarAsync_MessageIdYaGuardado_NoDuplicaYMantieneLaCantidad()
    {
        _repositorio.YaGuardados.Add(10);
        var fuente = FuenteMensajesFalsa.Con(new MensajeEntrante(ChatAutorizado, 10, "$10.000 sueldo de julio"));

        await Crear(fuente).EjecutarAsync(CancellationToken.None);

        Assert.Empty(_repositorio.Agregados);
    }

    // Regresión de FR-04: si la fuente repitiera un message_id dentro de la misma tanda,
    // ExisteAsync no lo vería —todavía no se confirmó— y el índice único rompería la corrida
    // entera al confirmar.
    [Fact]
    public async Task EjecutarAsync_MessageIdRepetidoEnLaMismaTanda_LoGuardaUnaSolaVez()
    {
        var fuente = FuenteMensajesFalsa.Con(
            new MensajeEntrante(ChatAutorizado, 10, "$10.000 sueldo"),
            new MensajeEntrante(ChatAutorizado, 10, "$10.000 sueldo"));

        await Crear(fuente).EjecutarAsync(CancellationToken.None);

        Assert.Single(_repositorio.Agregados);
    }

    // Sad path del error documentado: con el chat autorizado en 0 —el placeholder de
    // appsettings.json— el bot arranca pero no ingiere nada. Ni siquiera consulta la fuente.
    [Fact]
    public async Task EjecutarAsync_ChatAutorizadoEnCero_NoIngiereNada()
    {
        var fuente = FuenteMensajesFalsa.Con(new MensajeEntrante(ChatId: 0, MessageId: 10, Texto: "$5.000 nafta"));

        await Crear(fuente, chatAutorizado: 0).EjecutarAsync(CancellationToken.None);

        Assert.Empty(_repositorio.Agregados);
        Assert.Equal(0, fuente.Llamadas);
    }

    // Sad path del error documentado: si la fuente no responde, la corrida aborta sin guardar
    // nada. Los mensajes siguen en Telegram y se leen en la próxima corrida; ninguno queda
    // marcado con error, porque la falla no es del mensaje.
    [Fact]
    public async Task EjecutarAsync_FuenteLanzaExcepcion_NoGuardaNadaYNoMarcaError()
    {
        var fuente = FuenteMensajesFalsa.QueFalla(new InvalidOperationException("Telegram no responde"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Crear(fuente).EjecutarAsync(CancellationToken.None));

        Assert.Empty(_repositorio.Agregados);
        Assert.Equal(0, _unitOfWork.Confirmaciones);
    }

    // Sad path del error documentado: un update sin texto (foto, sticker) se descarta sin
    // guardar. No es un error.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EjecutarAsync_UpdateSinTexto_LoDescartaSinGuardar(string texto)
    {
        var fuente = FuenteMensajesFalsa.Con(new MensajeEntrante(ChatAutorizado, 10, texto));

        await Crear(fuente).EjecutarAsync(CancellationToken.None);

        Assert.Empty(_repositorio.Agregados);
    }

    // Sad path del error documentado: 4096 es el límite de Telegram. Un texto más largo se
    // trunca y se guarda; no se descarta y no rompe la corrida.
    [Fact]
    public async Task EjecutarAsync_TextoMayorA4096_LoTruncaYLoGuarda()
    {
        var texto = new string('t', 5000);
        var fuente = FuenteMensajesFalsa.Con(new MensajeEntrante(ChatAutorizado, 10, texto));

        await Crear(fuente).EjecutarAsync(CancellationToken.None);

        Assert.Equal(4096, Assert.Single(_repositorio.Agregados).Texto.Length);
    }

    // Sad path: un message_id no positivo no es un update válido de Telegram.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task EjecutarAsync_MessageIdNoPositivo_LoDescarta(long messageId)
    {
        var fuente = FuenteMensajesFalsa.Con(new MensajeEntrante(ChatAutorizado, messageId, "$5.000 nafta"));

        await Crear(fuente).EjecutarAsync(CancellationToken.None);

        Assert.Empty(_repositorio.Agregados);
    }

    // Valida M-04: la corrida procesa como máximo 100 mensajes y el resto queda en la fuente
    // para la próxima. Sin el tope, una tanda grande dispara N llamadas al modelo de hasta 15 s
    // cada una y la corrida se vuelve interminable.
    [Fact]
    public async Task EjecutarAsync_MasDeCienMensajes_ProcesaCienYDejaElRestoParaLaProximaCorrida()
    {
        var entrantes = Enumerable.Range(1, 150)
            .Select(i => new MensajeEntrante(ChatAutorizado, i, $"$1.000 gasto {i}"))
            .ToArray();
        var fuente = FuenteMensajesFalsa.Con(entrantes);

        await Crear(fuente).EjecutarAsync(CancellationToken.None);

        Assert.Equal(IngestarMensajes.MaximoPorCorrida, _repositorio.Agregados.Count);
        Assert.Equal(IngestarMensajes.MaximoPorCorrida, fuente.UltimoMaximoPedido);
    }

    // Sustenta la atomicidad de la ingesta: una sola confirmación por corrida, y sólo si hay
    // algo para guardar.
    [Fact]
    public async Task EjecutarAsync_SinMensajesParaGuardar_NoConfirma()
    {
        var fuente = FuenteMensajesFalsa.Con();

        await Crear(fuente).EjecutarAsync(CancellationToken.None);

        Assert.Equal(0, _unitOfWork.Confirmaciones);
    }
}
