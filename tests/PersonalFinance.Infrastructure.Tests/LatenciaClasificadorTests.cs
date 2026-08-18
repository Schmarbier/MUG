using System.Diagnostics;
using PersonalFinance.Domain.Clasificacion;
using PersonalFinance.Infrastructure.Ollama;
using PersonalFinance.Infrastructure.Tests.Datos;
using PersonalFinance.Infrastructure.Tests.Integracion;
using Xunit;
using Xunit.Abstractions;

namespace PersonalFinance.Infrastructure.Tests;

public class LatenciaClasificadorTests
{
    private static readonly TimeSpan MaximoP90 = TimeSpan.FromSeconds(5);

    private readonly ITestOutputHelper _salida;

    public LatenciaClasificadorTests(ITestOutputHelper salida) => _salida = salida;

    // Sad path del error documentado: si algún mensaje del dataset no se clasificó, el p90 se
    // estaría calculando sobre 49 de 50 y no sostendría lo que afirma AC-14. La muestra
    // incompleta falla; no se maquilla.
    [Fact]
    public void Latencia_AlgunMensajeDevuelveNoDisponible_FallaPorMuestraIncompleta()
    {
        Medicion[] mediciones =
        [
            new(1, TimeSpan.FromSeconds(1), Clasificado: true),
            new(2, TimeSpan.FromSeconds(2), Clasificado: false),
            new(3, TimeSpan.FromSeconds(3), Clasificado: true),
        ];

        var excepcion = Assert.Throws<InvalidOperationException>(
            () => MuestraLatencia.Percentil(mediciones, 90));

        Assert.Contains("muestra quedó incompleta", excepcion.Message, StringComparison.Ordinal);
        Assert.Contains("message_id 2", excepcion.Message, StringComparison.Ordinal);
    }

    // Regresión del cálculo: sobre una muestra completa, el p90 es el valor por debajo del cual
    // quedan el 90% de las mediciones.
    [Fact]
    public void Percentil_MuestraCompleta_DevuelveElValorDelPercentil()
    {
        var mediciones = Enumerable.Range(1, 10)
            .Select(i => new Medicion(i, TimeSpan.FromSeconds(i), Clasificado: true))
            .ToArray();

        Assert.Equal(TimeSpan.FromSeconds(9), MuestraLatencia.Percentil(mediciones, 90));
    }

    // Valida AC-14 (NFR-02): el p90 de la clasificación queda por debajo de 5 s. Los 50 mensajes
    // entran en la muestra, incluidos los lentos: el timeout de 15 s del adaptador está por
    // encima del umbral justamente para que una respuesta lenta se mida en vez de desaparecer.
    [Fact]
    [Trait("Categoria", "Integracion")]
    public async Task Latencia_SobreDatasetEtiquetado_P90MenorA5Segundos()
    {
        await OllamaDisponible.AsegurarAsync(OpcionesOllama.UriPorDefecto, CancellationToken.None);

        var dataset = DatasetEtiquetado.Cargar();
        var categorias = DatasetEtiquetado.Categorias();
        var clasificador = AccuracyClasificadorTests.CrearContraOllamaReal();
        var mediciones = new List<Medicion>(dataset.Count);

        foreach (var mensaje in dataset)
        {
            var cronometro = Stopwatch.StartNew();
            var resultado = await clasificador.ClasificarAsync(
                mensaje.Texto, categorias, CancellationToken.None);
            cronometro.Stop();

            mediciones.Add(new Medicion(
                mensaje.MessageId,
                cronometro.Elapsed,
                Clasificado: resultado is not ResultadoClasificacion.NoDisponible));
        }

        var p90 = MuestraLatencia.Percentil(mediciones, 90);

        _salida.WriteLine(
            $"p50 {MuestraLatencia.Percentil(mediciones, 50).TotalSeconds:F2}s · " +
            $"p90 {p90.TotalSeconds:F2}s (máximo 5s) · " +
            $"p99 {MuestraLatencia.Percentil(mediciones, 99).TotalSeconds:F2}s");

        Assert.True(
            p90 < MaximoP90,
            $"p90 {p90.TotalSeconds:F2}s — máximo 5s. " +
            $"p50 {MuestraLatencia.Percentil(mediciones, 50).TotalSeconds:F2}s, " +
            $"p99 {MuestraLatencia.Percentil(mediciones, 99).TotalSeconds:F2}s.");
    }
}
