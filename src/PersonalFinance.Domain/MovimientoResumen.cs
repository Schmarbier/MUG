namespace PersonalFinance.Domain;

// Proyección de un movimiento para el resumen (lo que la consulta trae de la base).
public record MovimientoResumen(
    TipoMovimiento Tipo, string CategoriaTitulo, string MonedaCodigo, decimal Monto, decimal? TipoCambioHistorico);
