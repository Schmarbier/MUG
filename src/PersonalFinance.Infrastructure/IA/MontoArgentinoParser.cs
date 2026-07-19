using System.Globalization;

namespace PersonalFinance.Infrastructure.IA;

/// <summary>
/// Convención argentina de escritura de montos (FR-041): punto separador de miles,
/// coma separadora de decimales. Un formato ambiguo se rechaza en vez de adivinarse
/// (Principio III) — el modelo solo copia el número tal cual aparece en el mensaje;
/// la interpretación numérica se hace acá, nunca confiando en la aritmética del modelo
/// (medido: "10,22" lo convertía en 1022 cuando se le pedía devolver un JSON number).
/// </summary>
public static class MontoArgentinoParser
{
    public static bool TryParsear(string? texto, out decimal monto)
    {
        monto = 0m;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpio = texto.Trim();
        if (limpio.Length == 0 || !limpio.All(c => char.IsDigit(c) || c is '.' or ','))
        {
            return false;
        }

        string parteEntera;
        string? parteDecimal;

        var indiceComa = limpio.LastIndexOf(',');
        if (indiceComa >= 0)
        {
            // La coma es siempre el separador decimal; cualquier punto antes es de miles.
            if (limpio.IndexOf(',', indiceComa + 1) >= 0)
            {
                return false; // más de una coma
            }

            parteDecimal = limpio[(indiceComa + 1)..];
            if (parteDecimal.Length is 0 or > 2)
            {
                return false;
            }

            parteEntera = limpio[..indiceComa].Replace(".", "");
        }
        else
        {
            var puntos = limpio.Count(c => c == '.');
            if (puntos == 0)
            {
                parteEntera = limpio;
                parteDecimal = null;
            }
            else if (puntos == 1)
            {
                var indicePunto = limpio.IndexOf('.');
                var despuesDelPunto = limpio[(indicePunto + 1)..];

                if (despuesDelPunto.Length == 3)
                {
                    // Único grupo de 3 dígitos tras el punto: separador de miles ("2.000" = 2000).
                    parteEntera = limpio.Replace(".", "");
                    parteDecimal = null;
                }
                else if (despuesDelPunto.Length is 1 or 2)
                {
                    // Un grupo de 1-2 dígitos no puede ser de miles: es decimal.
                    parteEntera = limpio[..indicePunto];
                    parteDecimal = despuesDelPunto;
                }
                else
                {
                    return false; // ambiguo
                }
            }
            else
            {
                // Más de un punto: todos son de miles, deben agrupar de a 3 dígitos.
                var partes = limpio.Split('.');
                if (partes.Skip(1).Any(p => p.Length != 3))
                {
                    return false;
                }

                parteEntera = string.Concat(partes);
                parteDecimal = null;
            }
        }

        if (parteEntera.Length == 0 || !parteEntera.All(char.IsDigit)
            || (parteDecimal is not null && !parteDecimal.All(char.IsDigit)))
        {
            return false;
        }

        var normalizado = parteDecimal is null ? parteEntera : $"{parteEntera}.{parteDecimal}";

        return decimal.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out monto);
    }
}
