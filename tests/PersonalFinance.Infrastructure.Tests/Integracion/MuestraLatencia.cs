namespace PersonalFinance.Infrastructure.Tests.Integracion;

/// <summary>Lo que tardó un mensaje del dataset y si llegó a clasificarse.</summary>
internal sealed record Medicion(long MessageId, TimeSpan Duracion, bool Clasificado);

/// <summary>
/// Percentiles sobre la muestra de latencia de NFR-02.
/// </summary>
internal static class MuestraLatencia
{
    /// <summary>
    /// Calcula el percentil pedido. Antes exige que la muestra esté completa: si algún mensaje
    /// no se clasificó, el percentil se estaría calculando sobre menos mensajes de los medidos y
    /// no sostendría la afirmación que hace AC-14. Por eso lanza en vez de devolver un número
    /// que parece válido.
    /// </summary>
    public static TimeSpan Percentil(IReadOnlyList<Medicion> mediciones, int percentil)
    {
        ArgumentNullException.ThrowIfNull(mediciones);
        ArgumentOutOfRangeException.ThrowIfLessThan(percentil, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentil, 100);

        if (mediciones.Count == 0)
        {
            throw new InvalidOperationException("No hay mediciones: la muestra está vacía.");
        }

        var incompletas = mediciones.Where(m => !m.Clasificado).Select(m => m.MessageId).ToArray();

        if (incompletas.Length > 0)
        {
            throw new InvalidOperationException(
                $"La muestra quedó incompleta: {incompletas.Length} de {mediciones.Count} mensajes " +
                $"no se clasificaron (message_id {string.Join(", ", incompletas)}). El p{percentil} " +
                "sobre una muestra incompleta no es el que pide el NFR.");
        }

        var ordenadas = mediciones.Select(m => m.Duracion).Order().ToArray();
        var indice = (int)Math.Ceiling(percentil / 100d * ordenadas.Length) - 1;

        return ordenadas[Math.Clamp(indice, 0, ordenadas.Length - 1)];
    }
}
