using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure.Tests;

public class ConsultaResumenTests
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

    [Fact] // Trae solo los movimientos del mes pedido, con categoría y moneda proyectadas.
    public async Task Trae_solo_los_movimientos_del_mes()
    {
        var (db, conexion) = CrearContexto();
        using (db)
        using (conexion)
        {
            var mensaje = new Mensaje { MessageId = 1, ChatId = 1, Texto = "x", FechaMensaje = DateTime.UtcNow, Procesado = true };
            db.Mensajes.Add(mensaje);
            db.SaveChanges();

            db.Movimientos.AddRange(
                new Movimiento { MensajeId = mensaje.Id, CategoriaId = 1, Tipo = TipoMovimiento.Egreso, Monto = 2000m, MonedaCodigo = "ARS", Fecha = new DateTime(2026, 7, 10) },
                new Movimiento { MensajeId = mensaje.Id, CategoriaId = 1, Tipo = TipoMovimiento.Egreso, Monto = 3000m, MonedaCodigo = "ARS", Fecha = new DateTime(2026, 7, 20) },
                new Movimiento { MensajeId = mensaje.Id, CategoriaId = 1, Tipo = TipoMovimiento.Egreso, Monto = 9999m, MonedaCodigo = "ARS", Fecha = new DateTime(2026, 6, 30) });
            db.SaveChanges();

            var delMes = await new ConsultaResumen(db).ObtenerMovimientosDelMesAsync(2026, 7);

            Assert.Equal(2, delMes.Count);
            Assert.All(delMes, m => Assert.Equal("Hogar", m.CategoriaTitulo));
            Assert.DoesNotContain(delMes, m => m.Monto == 9999m);
        }
    }
}
