using PersonalFinance.Domain.CasosDeUso;
using PersonalFinance.Domain.Clasificacion;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Tests.Dobles;
using Xunit;

namespace PersonalFinance.Domain.Tests;

public class ClasificarMensajesPendientesTests
{
    private static readonly Categoria Hogar = new Categoria("Hogar", "Gastos de la casa.").ConId(1);
    private static readonly Categoria Sueldo = new Categoria("Sueldo", "Ingresos por trabajo.").ConId(4);

    private readonly RepositorioMensajesEnMemoria _mensajes = new();
    private readonly RepositorioMovimientosEnMemoria _movimientos = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();

    private Mensaje Pendiente(string texto, long id = 1)
    {
        var mensaje = new Mensaje(messageId: id, texto, RelojFijo.Momento).ConId(id);
        _mensajes.Pendientes.Add(mensaje);

        return mensaje;
    }

    private ClasificarMensajesPendientes Crear(ClasificadorFalso clasificador, params Categoria[] activas) =>
        new(_mensajes,
            new RepositorioCategoriasFalso(activas),
            _movimientos,
            clasificador,
            _unitOfWork,
            new RelojFijo());

    // Valida AC-06: "$10.000 sueldo de julio" crea un movimiento de ingreso en Sueldo y deja el
    // mensaje procesado (FR-10).
    [Fact]
    public async Task EjecutarAsync_MensajeDeSueldo_CreaMovimientoIngresoEnSueldoYMarcaProcesado()
    {
        var mensaje = Pendiente("$10.000 sueldo de julio");
        var clasificador = ClasificadorFalso.Con(
            new ResultadoClasificacion.Clasificado(10_000m, TipoMovimiento.Ingreso, Sueldo));

        await Crear(clasificador, Hogar, Sueldo).EjecutarAsync(CancellationToken.None);

        var movimiento = Assert.Single(_movimientos.Agregados);
        Assert.Equal((10_000m, TipoMovimiento.Ingreso, Sueldo.Id, mensaje.Id),
            (movimiento.Monto, movimiento.Tipo, movimiento.CategoriaId, movimiento.MensajeId));
        Assert.True(mensaje.Procesado);
    }

    // Valida AC-07: "$2.000 comida casa" crea un movimiento de egreso en Hogar.
    [Fact]
    public async Task EjecutarAsync_MensajeDeComida_CreaMovimientoEgresoEnHogar()
    {
        Pendiente("$2.000 comida casa");
        var clasificador = ClasificadorFalso.Con(
            new ResultadoClasificacion.Clasificado(2_000m, TipoMovimiento.Egreso, Hogar));

        await Crear(clasificador, Hogar, Sueldo).EjecutarAsync(CancellationToken.None);

        var movimiento = Assert.Single(_movimientos.Agregados);
        Assert.Equal((2_000m, TipoMovimiento.Egreso, Hogar.Id),
            (movimiento.Monto, movimiento.Tipo, movimiento.CategoriaId));
    }

    // Valida AC-09: sin monto no hay movimiento; el mensaje queda con error y su motivo.
    [Fact]
    public async Task EjecutarAsync_SinMonto_MarcaErrorNoContieneMontoYNoCreaMovimiento()
    {
        var mensaje = Pendiente("compre cosas en el super");

        await Crear(ClasificadorFalso.Con(new ResultadoClasificacion.SinMonto()), Hogar)
            .EjecutarAsync(CancellationToken.None);

        Assert.Empty(_movimientos.Agregados);
        Assert.True(mensaje.Error);
        Assert.Equal(ClasificarMensajesPendientes.MotivoSinMonto, mensaje.Motivo);
    }

    // Valida AC-10.
    [Fact]
    public async Task EjecutarAsync_SinDescripcion_MarcaErrorNoContieneDescripcionYNoCreaMovimiento()
    {
        var mensaje = Pendiente("$3.000");

        await Crear(ClasificadorFalso.Con(new ResultadoClasificacion.SinDescripcion()), Hogar)
            .EjecutarAsync(CancellationToken.None);

        Assert.Empty(_movimientos.Agregados);
        Assert.Equal(ClasificarMensajesPendientes.MotivoSinDescripcion, mensaje.Motivo);
    }

    // Valida AC-11.
    [Fact]
    public async Task EjecutarAsync_TipoNoReconocido_MarcaErrorTipoNoReconocidoYNoCreaMovimiento()
    {
        var mensaje = Pendiente("transferi $5.000 a la cuenta de ahorro");

        await Crear(ClasificadorFalso.Con(new ResultadoClasificacion.TipoNoReconocido()), Hogar)
            .EjecutarAsync(CancellationToken.None);

        Assert.Empty(_movimientos.Agregados);
        Assert.Equal(ClasificarMensajesPendientes.MotivoTipoNoReconocido, mensaje.Motivo);
    }

    // Valida AC-12 (FR-12): si el clasificador no está disponible, el mensaje queda intacto y lo
    // levanta la próxima corrida. No se marca error: la falla no es del mensaje.
    [Fact]
    public async Task EjecutarAsync_ClasificadorNoDisponible_DejaElMensajeIntacto()
    {
        var mensaje = Pendiente("$10.000 sueldo de julio");

        await Crear(ClasificadorFalso.Con(new ResultadoClasificacion.NoDisponible()), Hogar)
            .EjecutarAsync(CancellationToken.None);

        Assert.Empty(_movimientos.Agregados);
        Assert.False(mensaje.Procesado);
        Assert.False(mensaje.Error);
        Assert.Equal(0, _unitOfWork.Confirmaciones);
    }

    // Sad path del error documentado: sin categorías activas no hay clasificación posible.
    // Marcar error en todos sería destruir datos recuperables, así que aborta sin tocar nada.
    [Fact]
    public async Task EjecutarAsync_SinCategoriasActivas_AbortaSinTocarMensajes()
    {
        var mensaje = Pendiente("$10.000 sueldo de julio");
        var clasificador = ClasificadorFalso.Con(new ResultadoClasificacion.SinMonto());

        var resultado = await Crear(clasificador).EjecutarAsync(CancellationToken.None);

        Assert.True(resultado.Abortada);
        Assert.Equal(0, clasificador.Llamadas);
        Assert.False(mensaje.Error);
        Assert.False(mensaje.Procesado);
    }

    // Sad path del error documentado: si la confirmación falla, no queda ni movimiento ni cambio
    // de estado persistido, y la corrida se corta en vez de arrastrar cambios pendientes a la
    // confirmación del mensaje siguiente.
    [Fact]
    public async Task EjecutarAsync_FallaAlPersistirElMovimiento_NoDejaMensajeMarcado()
    {
        Pendiente("$10.000 sueldo de julio", id: 1);
        var segundo = Pendiente("$2.000 comida casa", id: 2);
        _unitOfWork.FallaAlConfirmar = new InvalidOperationException("no se pudo guardar");
        var clasificador = ClasificadorFalso.Con(
            new ResultadoClasificacion.Clasificado(10_000m, TipoMovimiento.Ingreso, Sueldo));

        var resultado = await Crear(clasificador, Hogar, Sueldo).EjecutarAsync(CancellationToken.None);

        Assert.True(resultado.Abortada);
        Assert.Equal(0, resultado.Clasificados);
        Assert.Equal(1, _unitOfWork.Confirmaciones);
        Assert.False(segundo.Procesado);
    }

    // Regresión de FR-10: un mensaje ya procesado no vuelve a clasificarse. Es lo que evita que
    // cada corrida duplique movimientos.
    [Fact]
    public async Task EjecutarAsync_MensajeYaProcesado_NoLoVuelveAClasificar()
    {
        Pendiente("$10.000 sueldo de julio").MarcarProcesado();
        var clasificador = ClasificadorFalso.Con(
            new ResultadoClasificacion.Clasificado(10_000m, TipoMovimiento.Ingreso, Sueldo));

        await Crear(clasificador, Hogar, Sueldo).EjecutarAsync(CancellationToken.None);

        Assert.Equal(0, clasificador.Llamadas);
        Assert.Empty(_movimientos.Agregados);
    }

    // Regresión de FR-08: al clasificador sólo le llegan las categorías activas.
    [Fact]
    public async Task EjecutarAsync_ConCategoriasActivas_SeLasPasaAlClasificador()
    {
        Pendiente("$10.000 sueldo de julio");
        var clasificador = ClasificadorFalso.Con(
            new ResultadoClasificacion.Clasificado(10_000m, TipoMovimiento.Ingreso, Sueldo));

        await Crear(clasificador, Hogar, Sueldo).EjecutarAsync(CancellationToken.None);

        Assert.Equal([Hogar, Sueldo], clasificador.UltimasCategoriasRecibidas);
    }
}
