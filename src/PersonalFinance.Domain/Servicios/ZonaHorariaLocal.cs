namespace PersonalFinance.Domain.Servicios;

/// <summary>
/// Se persiste siempre en UTC (FechaRecepcionUtc); la fecha del movimiento se deriva
/// convirtiendo a la zona local recién en el momento de calcularla (R5).
/// </summary>
public static class ZonaHorariaLocal
{
    private static readonly TimeZoneInfo BuenosAires = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Argentina Standard Time" : "America/Argentina/Buenos_Aires");

    public static DateOnly DerivarFechaLocal(DateTimeOffset fechaUtc)
    {
        var local = TimeZoneInfo.ConvertTime(fechaUtc, BuenosAires);
        return DateOnly.FromDateTime(local.DateTime);
    }
}
