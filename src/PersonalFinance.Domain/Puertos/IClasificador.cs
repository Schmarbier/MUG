using PersonalFinance.Domain.Clasificacion;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Puertos;

/// <summary>
/// Puerto del agente que traduce el texto de un mensaje a monto, tipo y categoría. El dominio no
/// sabe que del otro lado hay un modelo corriendo en Ollama.
/// </summary>
public interface IClasificador
{
    /// <summary>
    /// Clasifica <paramref name="texto"/> contra las categorías activas. No lanza por los caminos
    /// de error del PRD: los devuelve como <see cref="ResultadoClasificacion"/>.
    /// </summary>
    Task<ResultadoClasificacion> ClasificarAsync(
        string texto,
        IReadOnlyList<Categoria> categoriasActivas,
        CancellationToken cancellationToken);
}
