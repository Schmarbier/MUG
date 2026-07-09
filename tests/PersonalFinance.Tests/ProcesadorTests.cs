using PersonalFinance.Bot.Domain;
using PersonalFinance.Bot.Services;

namespace PersonalFinance.Tests;

public class ProcesadorTests
{
    private static async Task<(TestDb db, ProcesadorService proc)> ArmarAsync(params (long id, string texto)[] mensajes)
    {
        var db = new TestDb();
        var ingesta = new IngestaService(db.Context);
        foreach (var (id, texto) in mensajes)
            await ingesta.GuardarAsync(id, texto, DateTimeOffset.UtcNow);

        var proc = new ProcesadorService(db.Context, new MensajeParser(), new ClasificadorFake());
        return (db, proc);
    }

    // AC-03: "$10.000 ingreso" con categoria "Sueldo" -> movimiento sueldo/10000/ingreso.
    [Fact]
    public async Task Crea_movimiento_de_ingreso()
    {
        var (db, proc) = await ArmarAsync((1, "$10.000 ingreso"));
        using var _ = db;

        var creados = await proc.ProcesarPendientesAsync();

        Assert.Equal(1, creados);
        var mov = db.Context.Movimientos.Single();
        var cat = db.Context.Categorias.Single(c => c.Id == mov.CategoriaId);
        Assert.Equal("Sueldo", cat.Titulo);
        Assert.Equal(10000m, mov.Monto);
        Assert.Equal(TipoMovimiento.Ingreso, mov.Tipo);
    }

    // AC-04: "$2.000 comida casa" con categoria "Hogar" -> movimiento hogar/2000/egreso.
    [Fact]
    public async Task Crea_movimiento_de_egreso()
    {
        var (db, proc) = await ArmarAsync((2, "$2.000 comida casa"));
        using var _ = db;

        await proc.ProcesarPendientesAsync();

        var mov = db.Context.Movimientos.Single();
        var cat = db.Context.Categorias.Single(c => c.Id == mov.CategoriaId);
        Assert.Equal("Hogar", cat.Titulo);
        Assert.Equal(2000m, mov.Monto);
        Assert.Equal(TipoMovimiento.Egreso, mov.Tipo);
    }

    // AC-07: un mensaje clasificado correctamente queda con procesado = true.
    [Fact]
    public async Task Mensaje_valido_queda_procesado()
    {
        var (db, proc) = await ArmarAsync((3, "$500 comida"));
        using var _ = db;

        await proc.ProcesarPendientesAsync();

        var mensaje = db.Context.Mensajes.Single(m => m.MessageId == 3);
        Assert.True(mensaje.Procesado);
        Assert.False(mensaje.Error);
        Assert.Null(mensaje.Motivo);
    }

    // AC-05 a nivel procesador: sin monto -> error, motivo, sin movimiento.
    [Fact]
    public async Task Mensaje_sin_monto_queda_con_error()
    {
        var (db, proc) = await ArmarAsync((4, "comida casa"));
        using var _ = db;

        var creados = await proc.ProcesarPendientesAsync();

        Assert.Equal(0, creados);
        var mensaje = db.Context.Mensajes.Single(m => m.MessageId == 4);
        Assert.False(mensaje.Procesado);
        Assert.True(mensaje.Error);
        Assert.Equal(MensajeParser.MotivoNoContieneMonto, mensaje.Motivo);
        Assert.Empty(db.Context.Movimientos);
    }

    // No debe reprocesar lo ya procesado (idempotencia entre corridas).
    [Fact]
    public async Task Segunda_corrida_no_duplica_movimientos()
    {
        var (db, proc) = await ArmarAsync((5, "$1.000 nafta"));
        using var _ = db;

        await proc.ProcesarPendientesAsync();
        var creadosSegunda = await proc.ProcesarPendientesAsync();

        Assert.Equal(0, creadosSegunda);
        Assert.Single(db.Context.Movimientos);
    }
}
