using PersonalFinance.Bot.Domain;

namespace PersonalFinance.Bot.Services;

/// <summary>Categoría asignada y tipo del movimiento, resultado de clasificar un mensaje (RF-05).</summary>
public sealed record ResultadoClasificacion(int CategoriaId, TipoMovimiento Tipo);

/// <summary>
/// Clasifica la descripción de un movimiento en una de las categorías existentes y
/// determina si es ingreso o egreso (RF-05). Abstracción para poder cambiar el modelo
/// o testear con un doble sin depender de Ollama.
/// </summary>
public interface IClasificador
{
    Task<ResultadoClasificacion> ClasificarAsync(
        string descripcion,
        decimal monto,
        IReadOnlyList<Categoria> categorias,
        CancellationToken ct = default);
}
