using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Infrastructure.Persistencia;
using Xunit;

namespace PersonalFinance.Infrastructure.Tests;

public class RepositorioMensajesTests
{
    private static readonly DateTime FechaRecepcion = new(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);

    // Sustenta AC-03: la deduplicación de FR-04 se resuelve consultando por message_id.
    [Fact]
    public async Task ExisteAsync_MessageIdYaGuardado_DevuelveTrue()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        await using var contexto = baseDatos.NuevoContexto();
        contexto.Mensajes.Add(new Mensaje(messageId: 42, texto: "$10.000 sueldo", fechaRecepcion: FechaRecepcion));
        await contexto.SaveChangesAsync(CancellationToken.None);

        var existe = await new RepositorioMensajesEfCore(contexto).ExisteAsync(42, CancellationToken.None);

        Assert.True(existe);
    }

    // Sustenta AC-03 por el otro lado: un message_id que nunca se ingirió no existe.
    [Fact]
    public async Task ExisteAsync_MessageIdDesconocido_DevuelveFalse()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        await using var contexto = baseDatos.NuevoContexto();

        var existe = await new RepositorioMensajesEfCore(contexto).ExisteAsync(42, CancellationToken.None);

        Assert.False(existe);
    }

    // Sad path del índice único: aunque la deduplicación fallara en el caso de uso, la base no
    // deja entrar dos veces el mismo message_id.
    [Fact]
    public async Task Guardar_MessageIdDuplicado_ViolaConstraintUnique()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        await using var contexto = baseDatos.NuevoContexto();
        var repositorio = new RepositorioMensajesEfCore(contexto);
        await repositorio.AgregarAsync(
            new Mensaje(messageId: 42, texto: "$10.000 sueldo", fechaRecepcion: FechaRecepcion),
            CancellationToken.None);
        await contexto.SaveChangesAsync(CancellationToken.None);

        await repositorio.AgregarAsync(
            new Mensaje(messageId: 42, texto: "$10.000 sueldo otra vez", fechaRecepcion: FechaRecepcion),
            CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => contexto.SaveChangesAsync(CancellationToken.None));
    }

    // Sustenta FR-06: la corrida de clasificación levanta sólo los mensajes sin procesar y sin
    // error, que es la consulta que sostiene el índice IX_Mensaje_Procesado_Error.
    [Fact]
    public async Task ObtenerPendientesAsync_ConProcesadosYConError_DevuelveSoloLosPendientes()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        await using var contexto = baseDatos.NuevoContexto();
        var pendiente = new Mensaje(messageId: 1, texto: "pendiente", fechaRecepcion: FechaRecepcion);
        var procesado = new Mensaje(messageId: 2, texto: "procesado", fechaRecepcion: FechaRecepcion);
        procesado.MarcarProcesado();
        var conError = new Mensaje(messageId: 3, texto: "con error", fechaRecepcion: FechaRecepcion);
        conError.MarcarError("no contiene monto");
        contexto.Mensajes.AddRange(pendiente, procesado, conError);
        await contexto.SaveChangesAsync(CancellationToken.None);

        await using var verificacion = baseDatos.NuevoContexto();
        var pendientes = await new RepositorioMensajesEfCore(verificacion)
            .ObtenerPendientesAsync(CancellationToken.None);

        Assert.Equal([1L], pendientes.Select(m => m.MessageId));
    }
}
