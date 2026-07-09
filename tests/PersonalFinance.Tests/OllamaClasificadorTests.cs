using PersonalFinance.Bot.Domain;
using PersonalFinance.Bot.Services;

namespace PersonalFinance.Tests;

/// <summary>
/// Tests del método puro de interpretación de la respuesta del modelo (sin red).
/// El flujo end-to-end contra Ollama se valida manualmente en el paso de integración.
/// </summary>
public class OllamaClasificadorTests
{
    private static readonly List<Categoria> Categorias =
    [
        new() { Id = 1, Titulo = "Sueldo", Descripcion = "ingresos por sueldo" },
        new() { Id = 2, Titulo = "Hogar", Descripcion = "gastos del hogar" },
        new() { Id = 6, Titulo = "Otros", Descripcion = "sin categoría" },
    ];

    [Fact]
    public void Interpreta_categoria_y_tipo_de_un_json_limpio()
    {
        var r = OllamaClasificador.Interpretar(
            """{ "categoria": "Hogar", "tipo": "egreso" }""", Categorias);

        Assert.Equal(2, r.CategoriaId);
        Assert.Equal(TipoMovimiento.Egreso, r.Tipo);
    }

    [Fact]
    public void Interpreta_ingreso_aunque_el_json_venga_envuelto_en_texto()
    {
        var r = OllamaClasificador.Interpretar(
            """Claro, aquí tienes: { "categoria": "Sueldo", "tipo": "ingreso" } listo.""", Categorias);

        Assert.Equal(1, r.CategoriaId);
        Assert.Equal(TipoMovimiento.Ingreso, r.Tipo);
    }

    [Fact]
    public void Categoria_desconocida_cae_en_Otros()
    {
        var r = OllamaClasificador.Interpretar(
            """{ "categoria": "Viajes", "tipo": "egreso" }""", Categorias);

        Assert.Equal(6, r.CategoriaId);
    }
}
