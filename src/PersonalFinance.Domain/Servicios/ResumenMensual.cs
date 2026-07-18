using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Servicios;

public sealed record FilaResumen(
    string Categoria,
    string CodigoMoneda,
    bool EsMonedaBase,
    decimal TotalEnMoneda,
    decimal EquivalenteEnBase);

public sealed record BloqueResumen(
    TipoMovimiento Tipo,
    IReadOnlyList<FilaResumen> Filas,
    int PaginaActual,
    int TotalPaginas,
    decimal TotalGeneral);

public sealed record ResumenMensual(BloqueResumen Ingresos, BloqueResumen Egresos);
