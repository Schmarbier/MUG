using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Infrastructure;

namespace PersonalFinance.Infrastructure.Tests;

public class SeedTests
{
    // SQLite in-memory con la conexión abierta viva durante el test: más fiel
    // a la base real que el provider InMemory. EnsureCreated aplica el HasData.
    private static (AppDbContext db, SqliteConnection conexion) CrearContexto()
    {
        var conexion = new SqliteConnection("DataSource=:memory:");
        conexion.Open();
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conexion)
            .Options;
        var db = new AppDbContext(opciones);
        db.Database.EnsureCreated();
        return (db, conexion);
    }

    [Fact] // RF-24 / AC-25: ARS preexistente como moneda base
    public void Seed_incluye_ARS_como_moneda_base()
    {
        var (db, conexion) = CrearContexto();
        using (db)
        using (conexion)
        {
            var ars = db.Monedas.SingleOrDefault(m => m.Codigo == "ARS");

            Assert.NotNull(ars);
            Assert.True(ars!.EsBase);
        }
    }

    [Fact] // Seed inicial: categorías fijas activas para que la clasificación tenga dónde asignar
    public void Seed_incluye_cuatro_categorias_activas()
    {
        var (db, conexion) = CrearContexto();
        using (db)
        using (conexion)
        {
            var categorias = db.Categorias.OrderBy(c => c.Id).ToList();

            Assert.Equal(4, categorias.Count);
            Assert.All(categorias, c => Assert.True(c.Activa));
            Assert.Contains(categorias, c => c.Titulo == "Hogar");
        }
    }
}
