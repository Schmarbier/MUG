using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Clasificacion;

/// <summary>
/// Resultado de clasificar el texto de un mensaje. Es un tipo cerrado: los cinco casos son los
/// únicos posibles y están declarados acá adentro (el constructor base es privado, así que nadie
/// fuera de este archivo puede agregar un sexto).
/// Los caminos de error del PRD se modelan como valor de retorno, nunca como excepción.
/// </summary>
public abstract record ResultadoClasificacion
{
    private ResultadoClasificacion()
    {
    }

    /// <summary>El texto pudo convertirse en un movimiento.</summary>
    public sealed record Clasificado(decimal Monto, TipoMovimiento Tipo, Categoria Categoria) : ResultadoClasificacion;

    /// <summary>El texto no contiene un monto utilizable (AC-09).</summary>
    public sealed record SinMonto : ResultadoClasificacion;

    /// <summary>El texto no contiene una descripción utilizable (AC-10).</summary>
    public sealed record SinDescripcion : ResultadoClasificacion;

    /// <summary>El clasificador devolvió un tipo distinto de ingreso o egreso (AC-11).</summary>
    public sealed record TipoNoReconocido : ResultadoClasificacion;

    /// <summary>
    /// El clasificador no respondió: caída, timeout, red o respuesta ilegible (AC-12).
    /// Es una falla de infraestructura, no un dato malo del usuario: el mensaje queda intacto.
    /// </summary>
    public sealed record NoDisponible : ResultadoClasificacion;
}
