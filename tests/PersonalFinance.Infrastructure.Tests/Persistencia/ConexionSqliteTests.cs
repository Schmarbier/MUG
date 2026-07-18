using PersonalFinance.Infrastructure.Persistencia;

namespace PersonalFinance.Infrastructure.Tests.Persistencia;

public sealed class ConexionSqliteTests
{
    [Fact]
    public void Sin_override_arma_ruta_absoluta_bajo_LocalAppData()
    {
        var cadena = ConexionSqlite.ObtenerCadenaConexion();

        var rutaEsperada = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PersonalFinance",
            "personalfinance.db");

        Assert.Equal($"Data Source={rutaEsperada}", cadena);
    }

    [Fact]
    public void Con_override_explicito_lo_respeta_sin_modificarlo()
    {
        const string cadenaOverride = "Data Source=:memory:";

        var cadena = ConexionSqlite.ObtenerCadenaConexion(cadenaOverride);

        Assert.Equal(cadenaOverride, cadena);
    }
}
