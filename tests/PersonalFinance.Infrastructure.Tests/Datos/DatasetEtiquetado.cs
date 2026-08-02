using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Infrastructure.Tests.Datos;

/// <summary>
/// Un mensaje del dataset con la clasificación que se espera de él.
/// </summary>
internal sealed record MensajeEtiquetado(long MessageId, string Texto, string Categoria, string Tipo)
{
    public TipoMovimiento TipoEsperado =>
        string.Equals(Tipo, "ingreso", StringComparison.OrdinalIgnoreCase)
            ? TipoMovimiento.Ingreso
            : TipoMovimiento.Egreso;
}

/// <summary>
/// Carga el dataset etiquetado que sostiene NFR-01 (accuracy) y NFR-02 (latencia). Lo comparten
/// los dos tests de integración: una sola fuente de verdad y una sola forma de leerla.
/// </summary>
internal static class DatasetEtiquetado
{
    private const string Ruta = "Datos/mensajes-etiquetados.json";

    public const int EntradasEsperadas = 50;

    public const int MinimoPorCategoria = 8;

    /// <summary>Las 5 del seed. El dataset no puede etiquetar fuera de ese conjunto.</summary>
    public static readonly string[] CategoriasDelSeed = ["Hogar", "Ocio", "Servicios", "Sueldo", "Otros"];

    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static IReadOnlyList<MensajeEtiquetado> Cargar()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, Ruta);
        var json = File.ReadAllText(ruta);

        return JsonSerializer.Deserialize<List<MensajeEtiquetado>>(json, Opciones)
            ?? throw new InvalidOperationException($"El dataset {ruta} está vacío o no es una lista.");
    }

    /// <summary>
    /// Las categorías del seed como entidades, para pasárselas al clasificador con las mismas
    /// descripciones que usa la aplicación real.
    /// </summary>
    public static IReadOnlyList<Categoria> Categorias() =>
    [
        new("Hogar", "Gastos de la casa: comida, supermercado, alquiler, expensas y mantenimiento."),
        new("Ocio", "Salidas, restaurantes, entretenimiento, viajes, suscripciones y hobbies."),
        new("Servicios", "Luz, gas, agua, internet, telefonía, seguros e impuestos."),
        new("Sueldo", "Ingresos por trabajo: sueldo, aguinaldo, honorarios y pagos de clientes."),
        new("Otros", "Categoría de descarte: lo que no encaja en ninguna de las anteriores."),
    ];
}
