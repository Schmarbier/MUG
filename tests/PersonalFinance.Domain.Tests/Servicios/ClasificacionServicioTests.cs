using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Domain.Tests.Falsos;

namespace PersonalFinance.Domain.Tests.Servicios;

public sealed class ClasificacionServicioTests
{
    private readonly RepositorioCategoriaFalso _categorias = new();
    private readonly RepositorioMonedaFalso _monedas = new();
    private readonly RepositorioMensajeFalso _mensajes = new();
    private readonly RepositorioMovimientoFalso _movimientos = new();
    private readonly ClasificadorDeMensajesFalso _clasificador = new();
    private readonly ClasificacionServicio _servicio;

    public ClasificacionServicioTests()
    {
        _servicio = new ClasificacionServicio(_clasificador, _categorias, _monedas, _movimientos, _mensajes);
    }

    private Categoria AgregarCategoria(string titulo, bool activa = true)
    {
        var categoria = new Categoria { Titulo = titulo, Descripcion = "d", Activa = activa };
        _categorias.Categorias.Add(categoria);
        categoria.Id = _categorias.Categorias.Count;
        return categoria;
    }

    private Moneda AgregarMoneda(string codigo, bool esBase, decimal? tipoDeCambio, bool activa = true)
    {
        var moneda = new Moneda { Codigo = codigo, EsBase = esBase, Activa = activa, TipoDeCambio = tipoDeCambio };
        _monedas.Monedas.Add(moneda);
        moneda.Id = _monedas.Monedas.Count;
        return moneda;
    }

    private Mensaje AgregarMensaje(int intentos = 0)
    {
        var mensaje = new Mensaje
        {
            IdentificadorCanal = 1,
            Texto = "2000 en super",
            FechaRecepcionUtc = DateTimeOffset.UtcNow,
            Procesado = false,
            IntentosClasificacion = intentos,
            TieneError = false
        };
        _mensajes.Mensajes.Add(mensaje);
        mensaje.Id = _mensajes.Mensajes.Count;
        return mensaje;
    }

    [Fact]
    public async Task Clasificacion_exitosa_crea_movimiento_con_los_datos_del_resultado()
    {
        var hogar = AgregarCategoria("Hogar");
        var ars = AgregarMoneda("ARS", esBase: true, tipoDeCambio: null);
        var mensaje = AgregarMensaje();
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(2000.00m, TipoMovimiento.Egreso, "Hogar", "ARS"));

        await _servicio.ClasificarAsync(mensaje);

        var movimiento = Assert.Single(_movimientos.Movimientos);
        Assert.Equal(mensaje.Id, movimiento.MensajeId);
        Assert.Equal(hogar.Id, movimiento.CategoriaId);
        Assert.Equal(ars.Id, movimiento.MonedaId);
        Assert.Equal(2000.00m, movimiento.Monto);
        Assert.Equal(TipoMovimiento.Egreso, movimiento.Tipo);
    }

    [Fact]
    public async Task Sin_moneda_explicita_asigna_ARS_sin_tipo_de_cambio_historico()
    {
        AgregarCategoria("Hogar");
        var ars = AgregarMoneda("ARS", esBase: true, tipoDeCambio: null);
        var mensaje = AgregarMensaje();
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(2000.00m, TipoMovimiento.Egreso, "Hogar", CodigoMoneda: null));

        await _servicio.ClasificarAsync(mensaje);

        var movimiento = Assert.Single(_movimientos.Movimientos);
        Assert.Equal(ars.Id, movimiento.MonedaId);
        Assert.Null(movimiento.TipoDeCambioHistorico);
    }

    [Fact]
    public async Task Falla_clasificador_no_disponible_incrementa_intentos_y_mantiene_pendiente()
    {
        AgregarCategoria("Hogar");
        AgregarMoneda("ARS", esBase: true, tipoDeCambio: null);
        var mensaje = AgregarMensaje(intentos: 1);
        _clasificador.Resultado = new ResultadoClasificacion.Fallida(new Falla(MotivoFalla.ClasificadorNoDisponible));

        await _servicio.ClasificarAsync(mensaje);

        Assert.Equal(2, mensaje.IntentosClasificacion);
        Assert.False(mensaje.TieneError);
        Assert.False(mensaje.Procesado);
    }

    [Fact]
    public async Task Al_tercer_intento_fallido_pasa_a_error_clasificador_no_disponible()
    {
        AgregarCategoria("Hogar");
        AgregarMoneda("ARS", esBase: true, tipoDeCambio: null);
        var mensaje = AgregarMensaje(intentos: 2);
        _clasificador.Resultado = new ResultadoClasificacion.Fallida(new Falla(MotivoFalla.ClasificadorNoDisponible));

        await _servicio.ClasificarAsync(mensaje);

        Assert.Equal(3, mensaje.IntentosClasificacion);
        Assert.True(mensaje.TieneError);
        Assert.Equal("clasificador no disponible", mensaje.MotivoError);
    }

    [Theory]
    [InlineData(MotivoFalla.SinMonto, "no contiene monto")]
    [InlineData(MotivoFalla.SinDescripcion, "no contiene descripción")]
    [InlineData(MotivoFalla.MonedaNoSoportada, "moneda no soportada")]
    [InlineData(MotivoFalla.SinConfianza, "no se pudo determinar la categoría con confianza")]
    public async Task Fallas_de_contenido_son_terminales_al_primer_intento(MotivoFalla motivo, string motivoEsperado)
    {
        AgregarCategoria("Hogar");
        AgregarMoneda("ARS", esBase: true, tipoDeCambio: null);
        var mensaje = AgregarMensaje();
        _clasificador.Resultado = new ResultadoClasificacion.Fallida(new Falla(motivo));

        await _servicio.ClasificarAsync(mensaje);

        Assert.True(mensaje.TieneError);
        Assert.Equal(motivoEsperado, mensaje.MotivoError);
        Assert.Equal(0, mensaje.IntentosClasificacion);
        Assert.DoesNotContain(mensaje, await _mensajes.ListarPendientesAsync());
    }

    [Fact]
    public async Task Falla_sin_confianza_marca_error_de_inmediato_sin_incrementar_intentos()
    {
        AgregarCategoria("Hogar");
        AgregarMoneda("ARS", esBase: true, tipoDeCambio: null);
        var mensaje = AgregarMensaje(intentos: 0);
        _clasificador.Resultado = new ResultadoClasificacion.Fallida(new Falla(MotivoFalla.SinConfianza));

        await _servicio.ClasificarAsync(mensaje);

        Assert.Equal(0, mensaje.IntentosClasificacion);
        Assert.True(mensaje.TieneError);
        Assert.Equal("no se pudo determinar la categoría con confianza", mensaje.MotivoError);
    }

    [Fact]
    public async Task Mensaje_del_que_se_creo_un_movimiento_queda_marcado_procesado()
    {
        AgregarCategoria("Hogar");
        AgregarMoneda("ARS", esBase: true, tipoDeCambio: null);
        var mensaje = AgregarMensaje();
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(2000.00m, TipoMovimiento.Egreso, "Hogar", "ARS"));

        await _servicio.ClasificarAsync(mensaje);

        Assert.True(mensaje.Procesado);
        Assert.False(mensaje.TieneError);
    }

    [Fact]
    public async Task Sin_categorias_activas_va_a_error_sin_invocar_el_clasificador()
    {
        AgregarMoneda("ARS", esBase: true, tipoDeCambio: null);
        var mensaje = AgregarMensaje();

        await _servicio.ClasificarAsync(mensaje);

        Assert.False(_clasificador.FueInvocado);
        Assert.True(mensaje.TieneError);
    }

    [Fact]
    public async Task Categoria_desactivada_no_se_incluye_al_clasificar_mensajes_nuevos()
    {
        AgregarCategoria("Hogar");
        AgregarCategoria("Vieja", activa: false);
        AgregarMoneda("ARS", esBase: true, tipoDeCambio: null);
        var mensaje = AgregarMensaje();
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(2000.00m, TipoMovimiento.Egreso, "Hogar", "ARS"));

        await _servicio.ClasificarAsync(mensaje);

        var movimiento = Assert.Single(_movimientos.Movimientos);
        Assert.NotEqual(2, movimiento.CategoriaId); // no puede haberse asignado "Vieja"
    }
}
