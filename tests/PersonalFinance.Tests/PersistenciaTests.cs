using Microsoft.EntityFrameworkCore;
using PersonalFinance.Bot.Domain;

namespace PersonalFinance.Tests;

public class PersistenciaTests
{
    [Fact]
    public void Migracion_crea_la_base_con_el_seed_de_categorias()
    {
        using var db = new TestDb();

        var categorias = db.Context.Categorias.OrderBy(c => c.Id).ToList();

        Assert.Equal(6, categorias.Count);
        Assert.Contains(categorias, c => c.Titulo == "Hogar" && c.Descripcion == "gastos del hogar");
        Assert.Contains(categorias, c => c.Titulo == "Sueldo");
    }

    [Fact]
    public void Guarda_y_recupera_un_mensaje()
    {
        using var db = new TestDb();

        db.Context.Mensajes.Add(new Mensaje
        {
            MessageId = 42,
            Texto = "$2.000 comida casa",
            FechaRecibido = DateTimeOffset.UtcNow
        });
        db.Context.SaveChanges();

        var guardado = db.Context.Mensajes.Single(m => m.MessageId == 42);

        Assert.Equal("$2.000 comida casa", guardado.Texto);
        Assert.False(guardado.Procesado);
        Assert.False(guardado.Error);
        Assert.Null(guardado.Motivo);
    }
}
