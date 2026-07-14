using PersonalFinance.Domain;

namespace PersonalFinance.Domain.Tests;

public class ServicioProcesamientoTests
{
    private sealed class AgenteFake(ExtraccionMovimiento resultado) : IAgenteClasificador
    {
        public Task<ExtraccionMovimiento> ExtraerAsync(
            string texto, IReadOnlyList<Categoria> categoriasActivas, CancellationToken ct = default)
            => Task.FromResult(resultado);
    }

    private sealed class CategoriasFake : IRepositorioCategorias
    {
        public Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Categoria>>([new Categoria { Id = 1, Titulo = "Hogar", Activa = true }]);
    }

    private sealed class MonedasFake : IRepositorioMonedas
    {
        public Task<Moneda?> ObtenerAsync(string codigo, CancellationToken ct = default)
            => Task.FromResult<Moneda?>(null);
    }

    private sealed class MensajesFake(params Mensaje[] pendientes) : IRepositorioMensajes
    {
        public Task<bool> ExisteAsync(long messageId, CancellationToken ct = default) => Task.FromResult(false);
        public Task AgregarAsync(Mensaje mensaje, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Mensaje>> ObtenerNoProcesadosAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Mensaje>>(pendientes);
        public int Guardados { get; private set; }
        public Task GuardarCambiosAsync(CancellationToken ct = default) { Guardados++; return Task.CompletedTask; }
    }

    private sealed class MovimientosFake : IRepositorioMovimientos
    {
        public List<Movimiento> Agregados { get; } = new();
        public Task AgregarAsync(Movimiento movimiento, CancellationToken ct = default)
        {
            Agregados.Add(movimiento);
            return Task.CompletedTask;
        }
    }

    private static ServicioProcesamiento Armar(ExtraccionMovimiento ext, MensajesFake mensajes, MovimientosFake movimientos)
    {
        var clasificacion = new ServicioClasificacion(new AgenteFake(ext), new CategoriasFake(), new MonedasFake());
        return new ServicioProcesamiento(clasificacion, mensajes, movimientos);
    }

    [Fact] // AC-09 / RF-10: creado el movimiento, el mensaje queda procesado
    public async Task Mensaje_valido_queda_procesado_y_crea_movimiento()
    {
        var mensaje = new Mensaje { Id = 1, Texto = "$2.000 comida casa", FechaMensaje = new DateTime(2026, 7, 10) };
        var mensajes = new MensajesFake(mensaje);
        var movimientos = new MovimientosFake();
        var servicio = Armar(new ExtraccionMovimiento(2000m, "comida casa", TipoMovimiento.Egreso, "Hogar", null), mensajes, movimientos);

        await servicio.ProcesarPendientesAsync();

        Assert.True(mensaje.Procesado);
        Assert.False(mensaje.Error);
        Assert.Single(movimientos.Agregados);
    }

    [Fact] // AC-07 / RF-11: sin monto, el mensaje queda con error y motivo, sin movimiento
    public async Task Mensaje_sin_monto_queda_con_error_y_sin_movimiento()
    {
        var mensaje = new Mensaje { Id = 1, Texto = "comida casa", FechaMensaje = new DateTime(2026, 7, 10) };
        var mensajes = new MensajesFake(mensaje);
        var movimientos = new MovimientosFake();
        var servicio = Armar(new ExtraccionMovimiento(null, "comida casa", TipoMovimiento.Egreso, "Hogar", null), mensajes, movimientos);

        await servicio.ProcesarPendientesAsync();

        Assert.False(mensaje.Procesado);
        Assert.True(mensaje.Error);
        Assert.Equal("no contiene monto", mensaje.MotivoError);
        Assert.Empty(movimientos.Agregados);
    }
}
