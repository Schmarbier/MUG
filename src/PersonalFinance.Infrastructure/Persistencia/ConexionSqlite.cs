namespace PersonalFinance.Infrastructure.Persistencia;

/// <summary>
/// Ruta absoluta y estable, compartida por Bot y Web (restricción de la constitución, R10).
/// Una ruta relativa produciría un archivo distinto por proceso, porque el working directory
/// de `dotnet run --project` es el del proyecto, no el del repo.
/// </summary>
public static class ConexionSqlite
{
    public static string ObtenerCadenaConexion(string? cadenaOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(cadenaOverride))
        {
            return cadenaOverride;
        }

        var carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PersonalFinance");

        var ruta = Path.Combine(carpeta, "personalfinance.db");

        return $"Data Source={ruta}";
    }
}
