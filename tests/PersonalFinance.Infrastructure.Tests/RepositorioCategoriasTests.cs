using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure.Tests;

public class RepositorioCategoriasTests
{
    private static (AppDbContext db, SqliteConnection conexion) CrearContexto()
    {
        var conexion = new SqliteConnection("DataSource=:memory:");
        conexion.Open();
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conexion).Options);
        db.Database.EnsureCreated();
        return (db, conexion);
    }

    [Fact] // RF-23 / AC-24: las categorías desactivadas no participan de la clasificación
    public async Task ObtenerActivas_excluye_las_desactivadas()
    {
        var (db, conexion) = CrearContexto();
        using (db)
        using (conexion)
        {
            var hogar = db.Categorias.Single(c => c.Titulo == "Hogar");
            hogar.Activa = false;
            db.SaveChanges();

            var activas = await new RepositorioCategorias(db).ObtenerActivasAsync();

            Assert.DoesNotContain(activas, c => c.Titulo == "Hogar");
            Assert.All(activas, c => Assert.True(c.Activa));
        }
    }
}
