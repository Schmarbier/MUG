using PersonalFinance.Bot.Domain;
using PersonalFinance.Bot.Services;

namespace PersonalFinance.Tests;

/// <summary>
/// Clasificador de prueba: decide por reglas simples de palabra clave, sin Ollama.
/// Permite testear el flujo del procesador de forma determinística.
/// </summary>
public sealed class ClasificadorFake : IClasificador
{
    public Task<ResultadoClasificacion> ClasificarAsync(
        string descripcion, decimal monto, IReadOnlyList<Categoria> categorias, CancellationToken ct = default)
    {
        var texto = descripcion.ToLowerInvariant();

        var tipo = texto.Contains("ingreso") || texto.Contains("sueldo")
            ? TipoMovimiento.Ingreso
            : TipoMovimiento.Egreso;

        var titulo = tipo == TipoMovimiento.Ingreso ? "Sueldo"
            : texto.Contains("comida") || texto.Contains("casa") ? "Hogar"
            : "Otros";

        var categoria = categorias.First(c =>
            string.Equals(c.Titulo, titulo, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(new ResultadoClasificacion(categoria.Id, tipo));
    }
}
