namespace PersonalFinance.Domain;

// Puerto del agente LLM. La implementación (OllamaSharp) vive en Infrastructure y no se
// unit-testea; la lógica determinística que la rodea sí (ver ServicioClasificacion).
public interface IAgenteClasificador
{
    Task<ExtraccionMovimiento> ExtraerAsync(
        string texto, IReadOnlyList<Categoria> categoriasActivas, CancellationToken ct = default);
}
