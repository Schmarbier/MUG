using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Servicios;

/// <summary>
/// Agregación en memoria del mes en curso. El redondeo se aplica una única vez, sobre la
/// suma de valores en precisión completa —nunca sumando valores ya redondeados— porque
/// sumar redondeos parciales corrompe el resultado (R2, FR-040).
/// </summary>
public class ResumenMensualServicio(
    IMovimientoRepositorio movimientoRepositorio,
    ICategoriaRepositorio categoriaRepositorio,
    IMonedaRepositorio monedaRepositorio)
{
    public const int FilasPorPagina = 4;

    public async Task<ResumenMensual> ObtenerResumenAsync(
        int anio,
        int mes,
        int paginaIngresos,
        int paginaEgresos,
        CancellationToken ct = default)
    {
        var movimientos = await movimientoRepositorio.ListarPorMesAsync(anio, mes, ct);
        var categorias = (await categoriaRepositorio.ListarTodasAsync(ct)).ToDictionary(c => c.Id);
        var monedas = (await monedaRepositorio.ListarTodasAsync(ct)).ToDictionary(m => m.Id);

        var ingresos = ConstruirBloque(
            movimientos.Where(m => m.Tipo == TipoMovimiento.Ingreso), TipoMovimiento.Ingreso, paginaIngresos, categorias, monedas);
        var egresos = ConstruirBloque(
            movimientos.Where(m => m.Tipo == TipoMovimiento.Egreso), TipoMovimiento.Egreso, paginaEgresos, categorias, monedas);

        return new ResumenMensual(ingresos, egresos);
    }

    private static BloqueResumen ConstruirBloque(
        IEnumerable<Movimiento> movimientosDelBloque,
        TipoMovimiento tipo,
        int pagina,
        IReadOnlyDictionary<int, Categoria> categorias,
        IReadOnlyDictionary<int, Moneda> monedas)
    {
        var movimientos = movimientosDelBloque.ToList();

        // Total general sobre TODOS los movimientos del mes, independiente de la paginación (FR-012a).
        var totalGeneral = Redondear(movimientos.Sum(Equivalente));

        var filas = movimientos
            .GroupBy(m => (m.CategoriaId, m.MonedaId))
            .Select(g =>
            {
                var categoria = categorias[g.Key.CategoriaId];
                var moneda = monedas[g.Key.MonedaId];
                return new FilaResumen(
                    categoria.Titulo,
                    moneda.Codigo,
                    moneda.EsBase,
                    g.Sum(m => m.Monto),
                    Redondear(g.Sum(Equivalente)));
            })
            .OrderByDescending(f => f.EquivalenteEnBase)
            .ThenBy(f => f.Categoria, StringComparer.Ordinal)
            .ThenBy(f => f.CodigoMoneda, StringComparer.Ordinal)
            .ToList();

        var totalPaginas = Math.Max(1, (int)Math.Ceiling(filas.Count / (double)FilasPorPagina));
        var paginaValida = Math.Clamp(pagina, 1, totalPaginas);
        var filasPagina = filas.Skip((paginaValida - 1) * FilasPorPagina).Take(FilasPorPagina).ToList();

        return new BloqueResumen(tipo, filasPagina, paginaValida, totalPaginas, totalGeneral);
    }

    private static decimal Equivalente(Movimiento movimiento) =>
        movimiento.Monto * (movimiento.TipoDeCambioHistorico ?? 1m);

    private static decimal Redondear(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}
