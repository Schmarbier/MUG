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
    /// <summary>
    /// El modelo contesta con verbos, no con los sustantivos del dominio. Medido: con
    /// <c>ingreso</c>/<c>egreso</c> el campo salía casi sin mirar el texto —"Pagué la luz" daba
    /// ingreso—; con <c>salio</c>/<c>entro</c> acierta. Son términos contables poco frecuentes
    /// contra verbos que describen lo que la persona hizo, que es sobre lo que el modelo razona
    /// bien. La traducción a <see cref="Domain.Entidades.TipoMovimiento"/> es del adaptador: el
    /// dominio sigue hablando de ingreso y egreso.
    /// </summary>
    public const string Entro = "entro";

    public const string Salio = "salio";

    public static JsonObject Crear(IReadOnlyList<Categoria> categoriasActivas)
    {
        ArgumentNullException.ThrowIfNull(categoriasActivas);

        var titulos = new JsonArray();
        foreach (var categoria in categoriasActivas)
        {
            titulos.Add(categoria.Titulo);
        }

        // El orden de las propiedades no es cosmético: la decodificación restringida obliga al
        // modelo a emitirlas en este orden, así que cada campo se decide con lo ya escrito como
        // contexto. Con "tipo" adelante, el modelo tenía que elegir dirección antes de haber
        // razonado nada, y arrastraba la categoría detrás del error. Con la categoría primero
        // —que es lo que mejor resuelve— el resto queda anclado.
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["categoria"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = titulos,
                },
                ["monto"] = new JsonObject { ["type"] = "number" },
                ["tipo"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray(Salio, Entro),
                },
            },
            // Sin campo "descripcion": no hay dónde guardarla —Movimiento no la tiene— y usar
            // "el modelo dejó ese campo vacío" como señal de SinDescripcion resultó ser una
            // medición de la prolijidad del modelo, no del texto. La medición de accuracy lo
            // mostró: 8 de 13 fallos eran mensajes bien clasificados que caían acá.
            ["required"] = new JsonArray("categoria", "monto", "tipo"),
        };
    }
}
