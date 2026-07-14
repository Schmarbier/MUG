namespace PersonalFinance.Domain;

// Una fila del resumen: categoría + moneda, con su total nativo y el equivalente en ARS
// (usando el tipo de cambio histórico de cada movimiento).
public record FilaResumen(string CategoriaTitulo, string MonedaCodigo, decimal Total, decimal EquivalenteArs);

// Dos bloques independientes; ingresos y egresos nunca se netean entre sí (AC-41).
public record ResumenMensual(IReadOnlyList<FilaResumen> Ingresos, IReadOnlyList<FilaResumen> Egresos);
