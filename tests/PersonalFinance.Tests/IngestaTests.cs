using PersonalFinance.Bot.Services;

namespace PersonalFinance.Tests;

public class IngestaTests
{
    // AC-01: un mensaje nuevo queda almacenado con estado "no procesado".
    [Fact]
    public async Task Mensaje_nuevo_queda_guardado_como_no_procesado()
    {
        using var db = new TestDb();
        var ingesta = new IngestaService(db.Context);

        var resultado = await ingesta.GuardarAsync(100, "$10.000 ingreso", DateTimeOffset.UtcNow);

        Assert.Equal(ResultadoIngesta.Guardado, resultado);

        var mensaje = db.Context.Mensajes.Single(m => m.MessageId == 100);
        Assert.False(mensaje.Procesado);
        Assert.False(mensaje.Error);
        Assert.Null(mensaje.Motivo);
    }

    // Riesgo del PRD: el bot no debe re-guardar el mismo mensaje en cada corrida.
    [Fact]
    public async Task Mismo_MessageId_no_se_guarda_dos_veces()
    {
        using var db = new TestDb();
        var ingesta = new IngestaService(db.Context);

        var primero = await ingesta.GuardarAsync(200, "$500 super", DateTimeOffset.UtcNow);
        var segundo = await ingesta.GuardarAsync(200, "$500 super", DateTimeOffset.UtcNow);

        Assert.Equal(ResultadoIngesta.Guardado, primero);
        Assert.Equal(ResultadoIngesta.Duplicado, segundo);
        Assert.Equal(1, db.Context.Mensajes.Count(m => m.MessageId == 200));
    }
}
