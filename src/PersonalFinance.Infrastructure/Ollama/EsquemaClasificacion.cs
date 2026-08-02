using System.Text.Json.Nodes;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Infrastructure.Ollama;

/// <summary>
/// JSON schema de la respuesta del clasificador. Va en el campo <c>format</c> de la request, así
/// el modelo queda restringido a los dos valores de <c>tipo</c> y a las categorías activas: no
/// puede alucinar fuera del conjunto válido (M-01).
/// Aun así la respuesta se valida al parsearla: un schema es una instrucción al modelo, no una
/// garantía del runtime.
/// </summary>
public static class EsquemaClasificacion
{
    public const string Ingreso = "ingreso";
    public const string Egreso = "egreso";

    public static JsonObject Crear(IReadOnlyList<Categoria> categoriasActivas)
    {
        ArgumentNullException.ThrowIfNull(categoriasActivas);

        var titulos = new JsonArray();
        foreach (var categoria in categoriasActivas)
        {
            titulos.Add(categoria.Titulo);
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["monto"] = new JsonObject { ["type"] = "number" },
                ["tipo"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray(Ingreso, Egreso),
                },
                ["categoria"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = titulos,
                },
                ["descripcion"] = new JsonObject { ["type"] = "string" },
            },
            ["required"] = new JsonArray("monto", "tipo", "categoria", "descripcion"),
        };
    }
}
