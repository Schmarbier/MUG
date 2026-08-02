using System.Text;
using OllamaSharp;
using PersonalFinance.Domain.Clasificacion;
using PersonalFinance.Infrastructure.Ollama;
using PersonalFinance.Infrastructure.Tests.Datos;
using PersonalFinance.Infrastructure.Tests.Integracion;
using Xunit;
using Xunit.Abstractions;

namespace PersonalFinance.Infrastructure.Tests;

public class AccuracyClasificadorTests
{
    private const double AccuracyMinima = 0.80;

    private readonly ITestOutputHelper _salida;

    public AccuracyClasificadorTests(ITestOutputHelper salida) => _salida = salida;

    internal static ClasificadorOllama CrearContraOllamaReal()
    {
        var opciones = new OpcionesOllama(OpcionesOllama.UriPorDefecto, OpcionesOllama.ModeloPorDefecto);
        var http = new HttpClient { BaseAddress = opciones.Uri, Timeout = opciones.Timeout };

        return new ClasificadorOllama(new OllamaApiClient(http, opciones.Modelo), opciones);
    }

    // Sad path de forma: el dataset se valida antes de gastar un minuto llamando al modelo. Un
    // dataset con etiquetas inválidas mediría cualquier cosa y diría que es accuracy.
    [Fact]
    public void Dataset_TieneCincuentaEntradasValidas_CubriendoLasCincoCategorias()
    {
        var dataset = DatasetEtiquetado.Cargar();

        Assert.Equal(DatasetEtiquetado.EntradasEsperadas, dataset.Count);
        Assert.Equal(dataset.Count, dataset.Select(m => m.MessageId).Distinct().Count());
        Assert.All(dataset, m => Assert.Contains(m.Categoria, DatasetEtiquetado.CategoriasDelSeed));
        Assert.All(dataset, m => Assert.Contains(m.Tipo, (string[])["ingreso", "egreso"]));
        Assert.All(dataset, m => Assert.False(string.IsNullOrWhiteSpace(m.Texto)));

        foreach (var categoria in DatasetEtiquetado.CategoriasDelSeed)
        {
            Assert.True(
                dataset.Count(m => m.Categoria == categoria) >= DatasetEtiquetado.MinimoPorCategoria,
                $"La categoría {categoria} tiene menos de {DatasetEtiquetado.MinimoPorCategoria} mensajes.");
        }
    }

    // Sad path del error documentado: si Ollama no está levantado, el test lo dice con todas las
    // letras en vez de morir en un timeout opaco.
    [Fact]
    public async Task Accuracy_OllamaNoDisponible_FallaConMensajeExplicito()
    {
        // Puerto sin nada escuchando: es el escenario "me olvidé de levantar Ollama".
        var apagado = new Uri("http://127.0.0.1:11435");

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => OllamaDisponible.AsegurarAsync(apagado, CancellationToken.None));

        Assert.Contains("Ollama no responde", excepcion.Message, StringComparison.Ordinal);
        Assert.Contains("ollama serve", excepcion.Message, StringComparison.Ordinal);
    }

    // Valida AC-13 (NFR-01): sobre el dataset etiquetado, la clasificación acierta al menos el
    // 80%. Un mensaje que cae en Otros sin que su etiqueta sea Otros cuenta como error: es la
    // degradación que el PRD anticipó en sus riesgos.
    [Fact]
    [Trait("Categoria", "Integracion")]
    public async Task Accuracy_SobreDatasetEtiquetado_EsMayorOIgualA80Porciento()
    {
        await OllamaDisponible.AsegurarAsync(OpcionesOllama.UriPorDefecto, CancellationToken.None);

        var dataset = DatasetEtiquetado.Cargar();
        var categorias = DatasetEtiquetado.Categorias();
        var clasificador = CrearContraOllamaReal();
        var aciertos = 0;
        var confusion = new List<string>();

        foreach (var esperado in dataset)
        {
            var resultado = await clasificador.ClasificarAsync(
                esperado.Texto, categorias, CancellationToken.None);

            if (resultado is ResultadoClasificacion.Clasificado obtenido &&
                obtenido.Categoria.Titulo == esperado.Categoria &&
                obtenido.Tipo == esperado.TipoEsperado)
            {
                aciertos++;
                continue;
            }

            confusion.Add(resultado is ResultadoClasificacion.Clasificado fallido
                ? $"  #{esperado.MessageId}: esperaba {esperado.Categoria}/{esperado.Tipo}, " +
                  $"obtuvo {fallido.Categoria.Titulo}/{fallido.Tipo}"
                : $"  #{esperado.MessageId}: esperaba {esperado.Categoria}/{esperado.Tipo}, " +
                  $"obtuvo {resultado.GetType().Name}");
        }

        var accuracy = (double)aciertos / dataset.Count;

        // Se informa siempre, no sólo al fallar: el margen contra el mínimo es lo que dice si el
        // clasificador está holgado o al borde.
        _salida.WriteLine($"Accuracy {accuracy:P1} ({aciertos}/{dataset.Count}), mínimo {AccuracyMinima:P0}.");

        Assert.True(
            accuracy >= AccuracyMinima,
            new StringBuilder()
                .AppendLine($"Accuracy {accuracy:P1} — mínimo exigido {AccuracyMinima:P0}.")
                .AppendLine($"Aciertos: {aciertos} de {dataset.Count}. Fallos:")
                .AppendLine(string.Join(Environment.NewLine, confusion))
                .ToString());
    }
}
