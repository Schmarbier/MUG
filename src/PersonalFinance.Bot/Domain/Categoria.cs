namespace PersonalFinance.Bot.Domain;

/// <summary>Categoría a la que se asigna un movimiento (RF-03). Ej: "hogar", "sueldo".</summary>
public class Categoria
{
    public int Id { get; set; }

    /// <summary>Título de la categoría, ej. "Hogar".</summary>
    public required string Titulo { get; set; }

    /// <summary>Descripción, ej. "gastos del hogar".</summary>
    public required string Descripcion { get; set; }

    public ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
}
