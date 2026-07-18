using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Infrastructure.Persistencia;

namespace PersonalFinance.Infrastructure.Tests.Persistencia;

public sealed class SeedTests : IDisposable
{
    private readonly string _rutaBd;
    private readonly PersonalFinanceDbContext _contexto;

    public SeedTests()
    {
        _rutaBd = Path.Combine(Path.GetTempPath(), $"personalfinance-seed-{Guid.NewGuid():N}.db");
        var opciones = new DbContextOptionsBuilder<PersonalFinanceDbContext>()
            .UseSqlite($"Data Source={_rutaBd}")
            .Options;
        _contexto = new PersonalFinanceDbContext(opciones);
        _contexto.Database.Migrate();
    }

    [Fact]
    public void La_migracion_inicial_siembra_ARS_como_moneda_base()
    {
        var ars = _contexto.Monedas.Single(m => m.Codigo == "ARS");

        Assert.True(ars.EsBase);
        Assert.True(ars.Activa);
        Assert.Null(ars.TipoDeCambio);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_rutaBd)) File.Delete(_rutaBd);
    }
}
