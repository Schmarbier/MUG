namespace PersonalFinance.Domain;

// Arma el resumen mensual (RF-12): agrupa por categoría + moneda dentro de cada bloque
// (ingresos/egresos) y calcula el equivalente en ARS con el tipo de cambio histórico.
public class ServicioResumen
{
    private readonly IConsultaResumen _consulta;

    public ServicioResumen(IConsultaResumen consulta) => _consulta = consulta;

    public async Task<ResumenMensual> ObtenerAsync(int anio, int mes, CancellationToken ct = default)
        => Construir(await _consulta.ObtenerMovimientosDelMesAsync(anio, mes, ct));

    public static ResumenMensual Construir(IEnumerable<MovimientoResumen> movimientos)
    {
        var grupos = movimientos
            .GroupBy(m => (m.Tipo, m.CategoriaTitulo, m.MonedaCodigo))
            .Select(g => (
                g.Key.Tipo,
                Fila: new FilaResumen(
                    g.Key.CategoriaTitulo,
                    g.Key.MonedaCodigo,
                    g.Sum(x => x.Monto),
                    // La moneda base no lleva tipo de cambio histórico: factor 1.
                    g.Sum(x => x.Monto * (x.TipoCambioHistorico ?? 1m)))))
            .ToList();

        return new ResumenMensual(
            FiltrarOrdenar(grupos, TipoMovimiento.Ingreso),
            FiltrarOrdenar(grupos, TipoMovimiento.Egreso));
    }

    private static IReadOnlyList<FilaResumen> FiltrarOrdenar(
        IEnumerable<(TipoMovimiento Tipo, FilaResumen Fila)> grupos, TipoMovimiento tipo)
        => grupos.Where(g => g.Tipo == tipo)
            .Select(g => g.Fila)
            .OrderBy(f => f.CategoriaTitulo)
            .ThenBy(f => f.MonedaCodigo)
            .ToList();
}
