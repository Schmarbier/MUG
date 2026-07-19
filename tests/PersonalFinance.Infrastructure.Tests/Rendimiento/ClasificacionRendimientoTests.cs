using System.Diagnostics;
using OllamaSharp;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Infrastructure.IA;

namespace PersonalFinance.Infrastructure.Tests.Rendimiento;

/// <summary>
/// SC-002: clasificación de un mensaje &lt; 5 s p90, contra un Ollama real (no simulado).
/// No es parte de la corrida normal de `dotnet test` (ver [Fact(Skip)]).
/// </summary>
public sealed class ClasificacionRendimientoTests
{
    private static readonly IReadOnlyList<CategoriaActiva> Categorias =
    [
        new("Hogar", "Alquiler, expensas, limpieza y mantenimiento del hogar"),
        new("Supermercado", "Compras de comida y artículos de almacén"),
        new("Transporte", "Colectivo, subte, nafta, taxi, Uber"),
    ];

    private static readonly IReadOnlyList<MonedaActiva> Monedas = [new("ARS", true)];

    private static readonly string[] Mensajes =
    [
        "2000 en super", "5000 nafta", "12000 alquiler", "3000 colectivo y subte",
        "8000 verdulería y carnicería", "15000 expensas", "4000 uber al trabajo",
        "6000 productos de limpieza", "9000 changuito del super", "2500 taxi",
    ];

    [Fact(Skip = "Requiere Ollama corriendo con llama3.1. Ejecutado manualmente el 2026-07-18: p90 = 908 ms sobre 10 mensajes (rango 812-950 ms), muy por debajo del umbral de SC-002.")]
    public async Task Clasificacion_resuelve_en_menos_de_5s_p90()
    {
        var cliente = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.1");
        var adaptador = new OllamaClasificadorAdapter(cliente, "llama3.1", TimeSpan.FromSeconds(10));

        // Warm-up: carga el modelo en memoria antes de medir.
        await adaptador.ClasificarAsync(Mensajes[0], Categorias, Monedas);

        var duraciones = new List<double>();
        foreach (var mensaje in Mensajes)
        {
            var cronometro = Stopwatch.StartNew();
            await adaptador.ClasificarAsync(mensaje, Categorias, Monedas);
            cronometro.Stop();
            duraciones.Add(cronometro.Elapsed.TotalMilliseconds);
        }

        duraciones.Sort();
        var indiceP90 = (int)Math.Ceiling(0.90 * duraciones.Count) - 1;
        var p90 = duraciones[indiceP90];

        Console.WriteLine($"RESULTADO_SC002: p90={p90:F0}ms de {string.Join(", ", duraciones.Select(d => d.ToString("F0")))}");

        Assert.True(p90 < 5000, $"p90 fue {p90:F0} ms, se esperaba < 5000 ms.");
    }
}
