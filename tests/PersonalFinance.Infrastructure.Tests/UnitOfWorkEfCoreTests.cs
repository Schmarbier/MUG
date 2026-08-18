using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Infrastructure.Persistencia;
using Xunit;

namespace PersonalFinance.Infrastructure.Tests;

public class UnitOfWorkEfCoreTests
{
    private static readonly DateTime Fecha = new(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);

    // Valida la atomicidad que exige el Bloque 5: el movimiento y el nuevo estado del mensaje se
    // guardan juntos o no se guarda ninguno. Acá el movimiento viola el índice único de
    // MensajeId y el mensaje tiene que quedar sin marcar.
    [Fact]
    public async Task ConfirmarAsync_FallaAlGuardarElMovimiento_NoPersisteElCambioDeEstadoDelMensaje()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        long mensajeId;
        int categoriaId;

        await using (var preparacion = baseDatos.NuevoContexto())
        {
            var categoria = new Categoria("Sueldo", "Ingresos por trabajo.");
            var mensaje = new Mensaje(messageId: 42, texto: "$10.000 sueldo", fechaRecepcion: Fecha);
            preparacion.Categorias.Add(categoria);
            preparacion.Mensajes.Add(mensaje);
            await preparacion.SaveChangesAsync(CancellationToken.None);

            categoriaId = categoria.Id;
            mensajeId = mensaje.Id;

            preparacion.Movimientos.Add(
                Movimiento.Crear(mensajeId, categoriaId, 10_000m, TipoMovimiento.Ingreso, Fecha));
            await preparacion.SaveChangesAsync(CancellationToken.None);
        }

        await using (var contexto = baseDatos.NuevoContexto())
        {
            var repositorioMovimientos = new RepositorioMovimientosEfCore(contexto);
            var unitOfWork = new UnitOfWorkEfCore(contexto);
            var mensaje = await contexto.Mensajes.SingleAsync(m => m.Id == mensajeId, CancellationToken.None);
            mensaje.MarcarProcesado();
            await repositorioMovimientos.AgregarAsync(
                Movimiento.Crear(mensajeId, categoriaId, 99m, TipoMovimiento.Ingreso, Fecha),
                CancellationToken.None);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => unitOfWork.ConfirmarAsync(CancellationToken.None));
        }

        await using var verificacion = baseDatos.NuevoContexto();
        var persistido = await verificacion.Mensajes.SingleAsync(m => m.Id == mensajeId, CancellationToken.None);
        Assert.False(persistido.Procesado);
    }
}
