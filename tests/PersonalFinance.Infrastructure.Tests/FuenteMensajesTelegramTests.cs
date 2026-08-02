using System.Net;
using System.Text.Json;
using PersonalFinance.Infrastructure.Telegram;
using PersonalFinance.Infrastructure.Tests.Dobles;
using Telegram.Bot;
using Xunit;

namespace PersonalFinance.Infrastructure.Tests;

public class FuenteMensajesTelegramTests
{
    private const string Token = "123456789:AAHscodigosecretodelbot1234567890abc";
    private const long ChatAutorizado = 555;
    private const long Fecha = 1785000000;

    private static FuenteMensajesTelegram Crear(HandlerFalso handler)
    {
        var cliente = new TelegramBotClient(new TelegramBotClientOptions(Token), new HttpClient(handler));

        return new FuenteMensajesTelegram(cliente, new OpcionesTelegram(Token, ChatAutorizado));
    }

    private static string RespuestaCon(params object[] updates) =>
        JsonSerializer.Serialize(new { ok = true, result = updates });

    private static object UpdateDeTexto(int updateId, int messageId, string texto) => new
    {
        update_id = updateId,
        message = new
        {
            message_id = messageId,
            date = Fecha,
            chat = new { id = ChatAutorizado, type = "private" },
            text = texto,
        },
    };

    // Sustenta FR-01: el adaptador traduce los updates de Telegram a mensajes entrantes con su
    // chat, su message_id y su texto.
    [Fact]
    public async Task LeerAsync_UpdateDeTexto_LoDevuelveComoMensajeEntrante()
    {
        var handler = HandlerFalso.ConJson(RespuestaCon(UpdateDeTexto(1, 10, "$10.000 sueldo")));

        var mensajes = await Crear(handler).LeerAsync(100, CancellationToken.None);

        var mensaje = Assert.Single(mensajes);
        Assert.Equal((ChatAutorizado, 10L, "$10.000 sueldo"), (mensaje.ChatId, mensaje.MessageId, mensaje.Texto));
    }

    // Precondición de la API: el máximo de mensajes por corrida es el límite de M-04, que el
    // caso de uso fija en 100. Pedir cero o menos no es un caso de negocio —"no leas nada"— sino
    // un error del llamador, y se rechaza antes de gastar una llamada a Telegram.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task LeerAsync_MaximoMenorAUno_LanzaArgumentOutOfRangeException(int maximo)
    {
        var handler = HandlerFalso.ConJson(RespuestaCon());

        var excepcion = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => Crear(handler).LeerAsync(maximo, CancellationToken.None));

        Assert.Equal("maximo", excepcion.ParamName);
        Assert.Equal(0, handler.Llamadas);
    }

    // Sad path del error documentado: un update que no es mensaje de texto (foto, sticker,
    // audio) se descarta sin guardar. No es un error.
    [Fact]
    public async Task LeerAsync_UpdateSinTexto_LoDescarta()
    {
        var foto = new
        {
            update_id = 1,
            message = new
            {
                message_id = 10,
                date = Fecha,
                chat = new { id = ChatAutorizado, type = "private" },
                photo = new[] { new { file_id = "abc", file_unique_id = "u", width = 1, height = 1 } },
            },
        };
        var handler = HandlerFalso.ConJson(RespuestaCon(foto, UpdateDeTexto(2, 11, "$5.000 nafta")));

        var mensajes = await Crear(handler).LeerAsync(100, CancellationToken.None);

        Assert.Equal([11L], mensajes.Select(m => m.MessageId));
    }

    // Sad path del error documentado: el texto se trunca a 4096, el límite de Telegram, en el
    // borde del sistema. La entidad Mensaje rechaza cualquier cosa más larga.
    [Fact]
    public async Task LeerAsync_TextoMayorA4096_LoTrunca()
    {
        var handler = HandlerFalso.ConJson(RespuestaCon(UpdateDeTexto(1, 10, new string('t', 5000))));

        var mensajes = await Crear(handler).LeerAsync(100, CancellationToken.None);

        Assert.Equal(4096, Assert.Single(mensajes).Texto.Length);
    }

    // Sad path del error documentado: 401 es token inválido. Falla con mensaje explícito y no
    // reintenta: reintentar con un token que Telegram ya rechazó no cambia el resultado.
    [Fact]
    public async Task LeerAsync_ApiDevuelve401_FallaConMensajeExplicitoYNoReintenta()
    {
        var handler = HandlerFalso.ConEstado(
            HttpStatusCode.Unauthorized,
            """{"ok":false,"error_code":401,"description":"Unauthorized"}""");

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Crear(handler).LeerAsync(100, CancellationToken.None));

        // "TelegramBotToken" sólo aparece en el mensaje de la rama de 401: verifica que el 401
        // se reconoció como tal y no cayó en el catch genérico.
        Assert.Contains("TelegramBotToken", excepcion.Message, StringComparison.Ordinal);
        Assert.Contains("401", excepcion.Message, StringComparison.Ordinal);
        Assert.Equal(1, handler.Llamadas);
    }

    // Valida M-03: Telegram.Bot incluye la URL de la request en el texto de sus excepciones, y
    // esa URL lleva el token adentro. Se re-lanza sin el token y sin excepción interna, porque
    // el inner volvería a filtrarlo apenas alguien loguee el ToString().
    [Fact]
    public async Task LeerAsync_ExcepcionDeTelegram_SeRelanzaSinElTokenEnElMensaje()
    {
        var handler = HandlerFalso.QueFalla(
            new HttpRequestException($"No se pudo conectar a https://api.telegram.org/bot{Token}/getUpdates"));

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Crear(handler).LeerAsync(100, CancellationToken.None));

        Assert.DoesNotContain(Token, excepcion.ToString(), StringComparison.Ordinal);
    }

    // Sustenta FR-04 desde el borde: el offset avanza entre corridas, así Telegram no vuelve a
    // entregar lo ya leído. Es el estado de instancia que obliga a registrar el adaptador como
    // singleton.
    [Fact]
    public async Task LeerAsync_SegundaCorrida_AvanzaElOffsetMasAllaDelUltimoUpdate()
    {
        var handler = HandlerFalso.ConJson(
            RespuestaCon(UpdateDeTexto(7, 10, "$10.000 sueldo")),
            RespuestaCon());
        var fuente = Crear(handler);

        await fuente.LeerAsync(100, CancellationToken.None);
        await fuente.LeerAsync(100, CancellationToken.None);

        Assert.Contains("\"offset\":8", handler.Pedidos[1], StringComparison.Ordinal);
    }
}
