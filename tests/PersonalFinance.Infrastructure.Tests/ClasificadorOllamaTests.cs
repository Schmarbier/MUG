using System.Text.Json;
using OllamaSharp;
using PersonalFinance.Domain.Clasificacion;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Infrastructure.Ollama;
using PersonalFinance.Infrastructure.Tests.Dobles;
using Xunit;

namespace PersonalFinance.Infrastructure.Tests;

public class ClasificadorOllamaTests
{
    private static readonly Categoria[] Activas =
    [
        new("Hogar", "Gastos de la casa."),
        new("Sueldo", "Ingresos por trabajo."),
        new("Otros", "Categoría de descarte."),
    ];

    private static ClasificadorOllama Crear(HandlerFalso handler)
    {
        var opciones = new OpcionesOllama(OpcionesOllama.UriPorDefecto, OpcionesOllama.ModeloPorDefecto);
        var http = new HttpClient(handler) { BaseAddress = opciones.Uri, Timeout = opciones.Timeout };

        return new ClasificadorOllama(new OllamaApiClient(http, opciones.Modelo), opciones);
    }

    /// <summary>Respuesta de /api/chat con el JSON de la clasificación adentro del content.</summary>
    private static string RespuestaCon(object clasificacion) =>
        JsonSerializer.Serialize(new
        {
            model = OpcionesOllama.ModeloPorDefecto,
            created_at = "2026-08-02T12:00:00Z",
            message = new { role = "assistant", content = JsonSerializer.Serialize(clasificacion) },
            done = true,
            done_reason = "stop",
        });

    private static string RespuestaCruda(string contenido) =>
        JsonSerializer.Serialize(new
        {
            model = OpcionesOllama.ModeloPorDefecto,
            created_at = "2026-08-02T12:00:00Z",
            message = new { role = "assistant", content = contenido },
            done = true,
            done_reason = "stop",
        });

    private static Task<ResultadoClasificacion> ClasificarAsync(HandlerFalso handler) =>
        Crear(handler).ClasificarAsync("$10.000 sueldo de julio", Activas, CancellationToken.None);

    // Sustenta AC-06 y AC-07: una respuesta válida se convierte en monto, tipo y categoría.
    [Fact]
    public async Task ClasificarAsync_RespuestaValida_DevuelveClasificado()
    {
        var handler = HandlerFalso.ConJson(RespuestaCon(new
        {
            monto = 10000,
            tipo = "ingreso",
            categoria = "Sueldo",
            descripcion = "sueldo de julio",
        }));

        var resultado = await ClasificarAsync(handler);

        var clasificado = Assert.IsType<ResultadoClasificacion.Clasificado>(resultado);
        Assert.Equal((10000m, TipoMovimiento.Ingreso, "Sueldo"),
            (clasificado.Monto, clasificado.Tipo, clasificado.Categoria.Titulo));
    }

    // Valida AC-08: una categoría que no está entre las activas cae en Otros (FR-09). El schema
    // ya restringe al modelo, pero la red igual está puesta: un schema es una instrucción, no
    // una garantía del runtime.
    [Fact]
    public async Task ClasificarAsync_CategoriaFueraDeLasActivas_DevuelveOtros()
    {
        var handler = HandlerFalso.ConJson(RespuestaCon(new
        {
            monto = 3500,
            tipo = "egreso",
            categoria = "Cripto",
            descripcion = "compra de cripto",
        }));

        var resultado = await ClasificarAsync(handler);

        Assert.Equal("Otros", Assert.IsType<ResultadoClasificacion.Clasificado>(resultado).Categoria.Titulo);
    }

    // Sad path del error documentado: Ollama caído o conexión rechazada. No lanza: devuelve
    // NoDisponible, que el caso de uso trata distinto de un dato malo del usuario (FR-12).
    [Fact]
    public async Task ClasificarAsync_OllamaNoResponde_DevuelveNoDisponible()
    {
        var handler = HandlerFalso.QueFalla(new HttpRequestException("Connection refused"));

        Assert.IsType<ResultadoClasificacion.NoDisponible>(await ClasificarAsync(handler));
    }

    // Sad path del error documentado: pasados los 15 s, HttpClient cancela la operación. Que el
    // token del llamador no esté cancelado es lo que distingue el timeout de una cancelación
    // real, que sí debe propagarse.
    [Fact]
    public async Task ClasificarAsync_TimeoutSuperado_DevuelveNoDisponible()
    {
        var handler = HandlerFalso.QueFalla(new TaskCanceledException("timeout", new TimeoutException()));

        Assert.IsType<ResultadoClasificacion.NoDisponible>(await ClasificarAsync(handler));
    }

    // Sad path del error documentado: el modelo respondió cualquier cosa. No es un dato malo del
    // usuario, es una falla del clasificador.
    [Fact]
    public async Task ClasificarAsync_RespuestaNoParseable_DevuelveNoDisponible()
    {
        var handler = HandlerFalso.ConJson(RespuestaCruda("esto no es json, es una charla"));

        Assert.IsType<ResultadoClasificacion.NoDisponible>(await ClasificarAsync(handler));
    }

    // Sad path del error documentado: tipo fuera de {ingreso, egreso} (AC-11).
    [Fact]
    public async Task ClasificarAsync_TipoFueraDelEnum_DevuelveTipoNoReconocido()
    {
        var handler = HandlerFalso.ConJson(RespuestaCon(new
        {
            monto = 1000,
            tipo = "transferencia",
            categoria = "Hogar",
            descripcion = "movimiento raro",
        }));

        Assert.IsType<ResultadoClasificacion.TipoNoReconocido>(await ClasificarAsync(handler));
    }

    // Sad path del error documentado: sin monto utilizable no hay movimiento (AC-09).
    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public async Task ClasificarAsync_MontoAusenteONegativo_DevuelveSinMonto(int monto)
    {
        var handler = HandlerFalso.ConJson(RespuestaCon(new
        {
            monto,
            tipo = "egreso",
            categoria = "Hogar",
            descripcion = "compra",
        }));

        Assert.IsType<ResultadoClasificacion.SinMonto>(await ClasificarAsync(handler));
    }

    // Sad path del error documentado: el campo monto ni siquiera vino.
    [Fact]
    public async Task ClasificarAsync_MontoAusente_DevuelveSinMonto()
    {
        var handler = HandlerFalso.ConJson(RespuestaCon(new
        {
            tipo = "egreso",
            categoria = "Hogar",
            descripcion = "compra",
        }));

        Assert.IsType<ResultadoClasificacion.SinMonto>(await ClasificarAsync(handler));
    }

    // Sad path: el texto no describe ningún movimiento (AC-10).
    [Fact]
    public async Task ClasificarAsync_SinDescripcion_DevuelveSinDescripcion()
    {
        var handler = HandlerFalso.ConJson(RespuestaCon(new
        {
            monto = 1000,
            tipo = "egreso",
            categoria = "Hogar",
            descripcion = "",
        }));

        Assert.IsType<ResultadoClasificacion.SinDescripcion>(await ClasificarAsync(handler));
    }

    // Sad path del error documentado: clasificar sin categorías es un error de programación, no
    // un caso de negocio, así que lanza en vez de devolver un resultado.
    [Fact]
    public async Task ClasificarAsync_CategoriasActivasVacia_LanzaArgumentException()
    {
        var handler = HandlerFalso.ConJson(RespuestaCruda("{}"));

        var excepcion = await Assert.ThrowsAsync<ArgumentException>(
            () => Crear(handler).ClasificarAsync("$10.000 sueldo", [], CancellationToken.None));

        Assert.Equal("categoriasActivas", excepcion.ParamName);
    }

    // Valida M-01 desde el borde: la request lleva el JSON schema con las categorías activas,
    // que es lo que impide que el modelo conteste fuera del conjunto válido.
    [Fact]
    public async Task ClasificarAsync_Request_LlevaElSchemaConLasCategoriasActivas()
    {
        var handler = HandlerFalso.ConJson(RespuestaCon(new
        {
            monto = 10000,
            tipo = "ingreso",
            categoria = "Sueldo",
            descripcion = "sueldo",
        }));

        await ClasificarAsync(handler);

        var pedido = Assert.Single(handler.Pedidos);
        Assert.Contains("\"enum\"", pedido, StringComparison.Ordinal);
        Assert.Contains("Sueldo", pedido, StringComparison.Ordinal);
    }
}
