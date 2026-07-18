using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PersonalFinance.Infrastructure.Persistencia.Converters;

/// <summary>
/// Nulo en la moneda base (FR-032, FR-035); guardado como INTEGER en centésimos (R1).
/// </summary>
public class TipoDeCambioValueConverter() : ValueConverter<decimal?, long?>(
    tipoDeCambio => tipoDeCambio == null
        ? null
        : (long?)decimal.Round(tipoDeCambio.Value * 100m, 0, MidpointRounding.AwayFromZero),
    centesimos => centesimos == null ? null : (decimal?)(centesimos.Value / 100m));
