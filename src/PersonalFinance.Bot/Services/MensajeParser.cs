using System.Globalization;
using System.Text.RegularExpressions;

namespace PersonalFinance.Bot.Services;

/// <summary>Resultado de parsear el texto de un mensaje: monto + descripción, o un motivo de error.</summary>
public sealed class ResultadoParseo
{
    public bool EsValido { get; private init; }
    public decimal Monto { get; private init; }
    public string Descripcion { get; private init; } = "";
    public string? Motivo { get; private init; }

    public static ResultadoParseo Exito(decimal monto, string descripcion) =>
        new() { EsValido = true, Monto = monto, Descripcion = descripcion };

    public static ResultadoParseo Fallo(string motivo) =>
        new() { EsValido = false, Motivo = motivo };
}

/// <summary>
/// Extrae monto y descripción del texto crudo de un mensaje y valida su forma (RF-07).
/// No decide el tipo (ingreso/egreso) ni la categoría: eso es del clasificador (RF-05).
/// Formato de monto argentino: '.' separador de miles, ',' decimal; el '$' es opcional.
/// </summary>
public sealed class MensajeParser
{
    public const string MotivoNoContieneMonto = "no contiene monto";
    public const string MotivoNoContieneDescripcion = "no contiene descripcion";

    // Número argentino: 10.000 | 2.000,50 | 500 | 500,25 (con o sin '$').
    private static readonly Regex MontoRegex =
        new(@"^\d{1,3}(\.\d{3})*(,\d+)?$|^\d+(,\d+)?$", RegexOptions.Compiled);

    public ResultadoParseo Parse(string texto)
    {
        var tokens = (texto ?? "").Split(
            ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        decimal? monto = null;
        var descripcion = new List<string>();

        foreach (var token in tokens)
        {
            if (monto is null && TryParseMonto(token, out var valor))
                monto = valor;
            else
                descripcion.Add(token);
        }

        if (monto is null)
            return ResultadoParseo.Fallo(MotivoNoContieneMonto);

        var texto_desc = string.Join(' ', descripcion).Trim();
        if (texto_desc.Length == 0)
            return ResultadoParseo.Fallo(MotivoNoContieneDescripcion);

        return ResultadoParseo.Exito(monto.Value, texto_desc);
    }

    private static bool TryParseMonto(string token, out decimal monto)
    {
        monto = 0;
        var limpio = token.Replace("$", "").Trim();
        if (limpio.Length == 0 || !MontoRegex.IsMatch(limpio))
            return false;

        // Normaliza a formato invariante: saca miles ('.') y pasa decimal (',') a '.'.
        var normalizado = limpio.Replace(".", "").Replace(",", ".");
        return decimal.TryParse(
            normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out monto);
    }
}
