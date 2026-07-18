using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PersonalFinance.Infrastructure.Persistencia.Converters;

/// <summary>
/// SQLite no tiene tipo decimal (R1): el monto se guarda como INTEGER en centavos
/// y se expone al dominio como decimal.
/// </summary>
public class MontoValueConverter() : ValueConverter<decimal, long>(
    monto => (long)decimal.Round(monto * 100m, 0, MidpointRounding.AwayFromZero),
    centavos => centavos / 100m);
