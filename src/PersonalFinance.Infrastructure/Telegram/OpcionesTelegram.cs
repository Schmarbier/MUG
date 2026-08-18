namespace PersonalFinance.Infrastructure.Telegram;

/// <summary>
/// Configuración del canal de Telegram, ya validada por <c>AgregarTelegram</c>.
/// </summary>
public sealed record OpcionesTelegram(string Token, long ChatAutorizado)
{
    /// <summary>
    /// M-03 (threat model): el token no se loguea nunca. Un record imprime todas sus
    /// propiedades por defecto, así que alcanza con que alguien lo interpole en un log para
    /// filtrarlo. Se enmascara acá, en el tipo, y no en cada punto de logueo.
    /// </summary>
    public override string ToString() =>
        $"OpcionesTelegram {{ Token = {Enmascarar(Token)}, ChatAutorizado = {ChatAutorizado} }}";

    /// <summary>
    /// Deja visible sólo el id numérico del bot, que no es secreto y sirve para identificar
    /// contra qué bot está corriendo el proceso. El secreto es lo que va después de los dos
    /// puntos.
    /// </summary>
    private static string Enmascarar(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return "***";
        }

        var separador = token.IndexOf(':', StringComparison.Ordinal);

        return separador <= 0 ? "***" : $"{token[..separador]}:***";
    }
}
