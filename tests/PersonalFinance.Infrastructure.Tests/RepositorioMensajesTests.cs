using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure.Tests;

public class RepositorioMensajesTests
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

    [Fact] // El adaptador real sobre EF/SQLite persiste y detecta existencia por message_id.
    public async Task Agregar_persiste_el_mensaje_y_Existe_lo_detecta()
    {
        var (db, conexion) = CrearContexto();
        using (db)
        using (conexion)
        {
            var repo = new RepositorioMensajes(db);
            Assert.False(await repo.ExisteAsync(500));

            await repo.AgregarAsync(new Mensaje
            {
                MessageId = 500,
                ChatId = 1,
                Texto = "hola",
                FechaMensaje = DateTime.UtcNow,
            });

            Assert.True(await repo.ExisteAsync(500));
            Assert.Equal(1, db.Mensajes.Count());
        }
    }
}
