namespace PersonalFinance.Domain;

// Salida "cruda" del LLM: lo que el agente extrae del texto, antes de validar/persistir.
// Monto/Descripcion null => el modelo no los encontró. MonedaCodigo null => sin moneda explícita.
public record ExtraccionMovimiento(
    decimal? Monto,
    string? Descripcion,
    TipoMovimiento Tipo,
    string? CategoriaTitulo,
    string? MonedaCodigo);
