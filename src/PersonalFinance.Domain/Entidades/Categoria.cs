namespace PersonalFinance.Domain.Entidades;

/// <summary>
/// Agrupador de movimientos. Sólo las activas participan de la clasificación.
/// </summary>
public class Categoria
{
    /// <summary>Largo máximo del título.</summary>
    public const int TituloMaximo = 60;

    /// <summary>Largo máximo de la descripción.</summary>
    public const int DescripcionMaximo = 200;

    public Categoria(string titulo, string descripcion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(titulo.Length, TituloMaximo, nameof(titulo));
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(descripcion.Length, DescripcionMaximo, nameof(descripcion));

        Titulo = titulo;
        Descripcion = descripcion;
        Activa = true;
    }

    public int Id { get; private set; }

    public string Titulo { get; private set; }

    public string Descripcion { get; private set; }

    public bool Activa { get; private set; }
}
