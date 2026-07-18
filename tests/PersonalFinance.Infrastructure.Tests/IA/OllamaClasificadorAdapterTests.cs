using System.Net;
using OllamaSharp;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Infrastructure.IA;
using PersonalFinance.Infrastructure.Tests.Falsos;

namespace PersonalFinance.Infrastructure.Tests.IA;

public sealed class OllamaClasificadorAdapterTests
{
    private static readonly IReadOnlyList<CategoriaActiva> Categorias = [new("Hogar", "Gastos del hogar")];
    private static readonly IReadOnlyList<MonedaActiva> Monedas = [new("ARS", true), new("USD", false)];

    private static OllamaClasificadorAdapter CrearAdaptador(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder,
        TimeSpan? timeout = null)
    {
        var handler = new ManejadorHttpFalso(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var cliente = new OllamaApiClient(httpClient, "llama3.1");
        return new OllamaClasificadorAdapter(cliente, "llama3.1", timeout ?? TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Respuesta_valida_produce_una_clasificacion_exitosa()
    {
        var adaptador = CrearAdaptador((_, _) => Task.FromResult(
            ManejadorHttpFalso.RespuestaGenerate(
                """{"monto":2000.00,"tipo":"egreso","categoria":"Hogar","moneda":"ARS","confianza":0.92}""")));

        var resultado = await adaptador.ClasificarAsync("2000 en super", Categorias, Monedas);

        var exitosa = Assert.IsType<ResultadoClasificacion.Exitosa>(resultado);
        Assert.Equal(2000.00m, exitosa.Clasificacion.Monto);
        Assert.Equal(TipoMovimiento.Egreso, exitosa.Clasificacion.Tipo);
        Assert.Equal("Hogar", exitosa.Clasificacion.TituloCategoria);
        Assert.Equal("ARS", exitosa.Clasificacion.CodigoMoneda);
    }

    [Fact]
    public async Task Respuesta_sin_moneda_deja_CodigoMoneda_nulo()
    {
        var adaptador = CrearAdaptador((_, _) => Task.FromResult(
            ManejadorHttpFalso.RespuestaGenerate(
                """{"monto":2000.00,"tipo":"egreso","categoria":"Hogar","confianza":0.92}""")));

        var resultado = await adaptador.ClasificarAsync("2000 en super", Categorias, Monedas);

        var exitosa = Assert.IsType<ResultadoClasificacion.Exitosa>(resultado);
        Assert.Null(exitosa.Clasificacion.CodigoMoneda);
    }

    [Fact]
    public async Task Json_malformado_produce_falla_sin_confianza()
    {
        var adaptador = CrearAdaptador((_, _) => Task.FromResult(
            ManejadorHttpFalso.RespuestaGenerate("esto no es json")));

        var resultado = await adaptador.ClasificarAsync("2000 en super", Categorias, Monedas);

        var fallida = Assert.IsType<ResultadoClasificacion.Fallida>(resultado);
        Assert.Equal(MotivoFalla.SinConfianza, fallida.Falla.Motivo);
    }

    [Fact]
    public async Task Monto_ausente_produce_falla_sin_monto()
    {
        var adaptador = CrearAdaptador((_, _) => Task.FromResult(
            ManejadorHttpFalso.RespuestaGenerate(
                """{"tipo":"egreso","categoria":"Hogar","moneda":"ARS","confianza":0.92}""")));

        var resultado = await adaptador.ClasificarAsync("en super", Categorias, Monedas);

        var fallida = Assert.IsType<ResultadoClasificacion.Fallida>(resultado);
        Assert.Equal(MotivoFalla.SinMonto, fallida.Falla.Motivo);
    }

    [Fact]
    public async Task Categoria_inexistente_produce_falla_sin_confianza()
    {
        var adaptador = CrearAdaptador((_, _) => Task.FromResult(
            ManejadorHttpFalso.RespuestaGenerate(
                """{"monto":2000.00,"tipo":"egreso","categoria":"Categoria que no existe","moneda":"ARS","confianza":0.92}""")));

        var resultado = await adaptador.ClasificarAsync("2000 en algo raro", Categorias, Monedas);

        var fallida = Assert.IsType<ResultadoClasificacion.Fallida>(resultado);
        Assert.Equal(MotivoFalla.SinConfianza, fallida.Falla.Motivo);
    }

    [Fact]
    public async Task Moneda_no_reconocida_produce_falla_moneda_no_soportada()
    {
        var adaptador = CrearAdaptador((_, _) => Task.FromResult(
            ManejadorHttpFalso.RespuestaGenerate(
                """{"monto":100.00,"tipo":"egreso","categoria":"Hogar","moneda":"EUR","confianza":0.92}""")));

        var resultado = await adaptador.ClasificarAsync("100 EUR viaje", Categorias, Monedas);

        var fallida = Assert.IsType<ResultadoClasificacion.Fallida>(resultado);
        Assert.Equal(MotivoFalla.MonedaNoSoportada, fallida.Falla.Motivo);
    }

    [Fact]
    public async Task Confianza_bajo_el_umbral_produce_falla_sin_confianza()
    {
        var adaptador = CrearAdaptador((_, _) => Task.FromResult(
            ManejadorHttpFalso.RespuestaGenerate(
                """{"monto":2000.00,"tipo":"egreso","categoria":"Hogar","moneda":"ARS","confianza":1.5}""")));

        var resultado = await adaptador.ClasificarAsync("2000 en super", Categorias, Monedas);

        var fallida = Assert.IsType<ResultadoClasificacion.Fallida>(resultado);
        Assert.Equal(MotivoFalla.SinConfianza, fallida.Falla.Motivo);
    }

    [Fact]
    public async Task Timeout_produce_falla_clasificador_no_disponible()
    {
        var adaptador = CrearAdaptador(
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return ManejadorHttpFalso.RespuestaGenerate("{}");
            },
            timeout: TimeSpan.FromMilliseconds(50));

        var resultado = await adaptador.ClasificarAsync("2000 en super", Categorias, Monedas);

        var fallida = Assert.IsType<ResultadoClasificacion.Fallida>(resultado);
        Assert.Equal(MotivoFalla.ClasificadorNoDisponible, fallida.Falla.Motivo);
    }

    [Fact]
    public async Task Servidor_caido_produce_falla_clasificador_no_disponible()
    {
        var adaptador = CrearAdaptador((_, _) => throw new HttpRequestException("conexión rechazada"));

        var resultado = await adaptador.ClasificarAsync("2000 en super", Categorias, Monedas);

        var fallida = Assert.IsType<ResultadoClasificacion.Fallida>(resultado);
        Assert.Equal(MotivoFalla.ClasificadorNoDisponible, fallida.Falla.Motivo);
    }
}
