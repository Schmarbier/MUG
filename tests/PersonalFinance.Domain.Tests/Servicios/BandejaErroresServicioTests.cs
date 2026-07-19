using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Domain.Tests.Falsos;

namespace PersonalFinance.Domain.Tests.Servicios;

public sealed class BandejaErroresServicioTests
{
    private readonly RepositorioMensajeFalso _mensajes = new();
    private readonly RepositorioCategoriaFalso _categorias = new();
    private readonly RepositorioMonedaFalso _monedas = new();
    private readonly RepositorioMovimientoFalso _movimientos = new();
    private readonly ClasificadorDeMensajesFalso _clasificador = new();
    private readonly ClasificacionServicio _clasificacionServicio;
    private readonly BandejaErroresServicio _servicio;

    public BandejaErroresServicioTests()
    {
        _clasificacionServicio = new ClasificacionServicio(_clasificador, _categorias, _monedas, _movimientos, _mensajes);
        _servicio = new BandejaErroresServicio(_mensajes, _clasificacionServicio);
    }

    private Mensaje AgregarMensajeConError(string motivo, string texto = "texto")
    {
        var mensaje = new Mensaje
        {
            Id = _mensajes.Mensajes.Count + 1,
            IdentificadorCanal = _mensajes.Mensajes.Count + 1,
            Texto = texto,
            FechaRecepcionUtc = DateTimeOffset.UtcNow,
            Procesado = false,
            IntentosClasificacion = 0,
            TieneError = true,
            MotivoError = motivo
        };
        _mensajes.Mensajes.Add(mensaje);
        return mensaje;
    }

    [Theory]
    [InlineData("no contiene monto")]
    [InlineData("no contiene descripción")]
    [InlineData("moneda no soportada")]
    public async Task Mensajes_con_error_aparecen_listados_con_su_motivo(string motivo)
    {
        AgregarMensajeConError(motivo);

        var listado = await _servicio.ListarAsync();

        Assert.Single(listado);
        Assert.Equal(motivo, listado[0].MotivoError);
    }

    [Fact]
    public async Task Reprocesar_tras_corregir_la_causa_lo_deja_procesado_con_su_movimiento()
    {
        var categoria = new Categoria { Id = 1, Titulo = "Hogar", Descripcion = "d", Activa = true };
        _categorias.Categorias.Add(categoria);
        var ars = new Moneda { Id = 1, Codigo = "ARS", EsBase = true, Activa = true, TipoDeCambio = null };
        _monedas.Monedas.Add(ars);
        var mensaje = AgregarMensajeConError("moneda no soportada");
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(1000.00m, TipoMovimiento.Egreso, "Hogar", "ARS"));

        await _servicio.ReprocesarAsync(mensaje.Id);

        Assert.True(mensaje.Procesado);
        Assert.False(mensaje.TieneError);
        Assert.Single(_movimientos.Movimientos);
    }

    [Fact]
    public async Task Reprocesar_un_mensaje_ya_procesado_no_duplica_el_movimiento()
    {
        var mensaje = AgregarMensajeConError("moneda no soportada");
        mensaje.TieneError = false;
        mensaje.Procesado = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.ReprocesarAsync(mensaje.Id));

        Assert.Empty(_movimientos.Movimientos);
    }

    private void AgregarCatalogoBase()
    {
        _categorias.Categorias.Add(new Categoria { Id = 1, Titulo = "Hogar", Descripcion = "d", Activa = true });
        _monedas.Monedas.Add(new Moneda { Id = 1, Codigo = "ARS", EsBase = true, Activa = true, TipoDeCambio = null });
    }

    [Fact]
    public async Task Reprocesar_todos_resuelve_toda_la_bandeja_cuando_la_causa_esta_corregida()
    {
        AgregarCatalogoBase();
        var uno = AgregarMensajeConError("moneda no soportada", "1000 uno");
        var dos = AgregarMensajeConError("moneda no soportada", "1000 dos");
        var tres = AgregarMensajeConError("moneda no soportada", "1000 tres");
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(1000.00m, TipoMovimiento.Egreso, "Hogar", "ARS"));

        var resultado = await _servicio.ReprocesarTodosAsync();

        Assert.Equal(3, resultado.Total);
        Assert.Equal(3, resultado.Exitosos);
        Assert.Equal(0, resultado.ConError);
        Assert.All([uno, dos, tres], m => Assert.True(m.Procesado));
        Assert.Empty(await _servicio.ListarAsync());
        Assert.Equal(3, _movimientos.Movimientos.Count);
    }

    [Fact]
    public async Task Reprocesar_todos_sigue_con_el_resto_aunque_alguno_vuelva_a_fallar()
    {
        AgregarCatalogoBase();
        var bueno = AgregarMensajeConError("moneda no soportada", "1000 bueno");
        var malo = AgregarMensajeConError("moneda no soportada", "sin monto");
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(1000.00m, TipoMovimiento.Egreso, "Hogar", "ARS"));
        _clasificador.ResultadoPorTexto["sin monto"] =
            new ResultadoClasificacion.Fallida(new Falla(MotivoFalla.SinMonto));

        var resultado = await _servicio.ReprocesarTodosAsync();

        Assert.Equal(2, resultado.Total);
        Assert.Equal(1, resultado.Exitosos);
        Assert.Equal(1, resultado.ConError);
        Assert.True(bueno.Procesado);
        Assert.True(malo.TieneError);
        Assert.Equal("no contiene monto", malo.MotivoError);
        // El que falló sigue en la bandeja; el que anduvo salió.
        var restantes = await _servicio.ListarAsync();
        Assert.Same(malo, Assert.Single(restantes));
    }

    [Fact]
    public async Task Reprocesar_todos_deja_el_mensaje_en_la_bandeja_persistido_aunque_falle_el_guardado()
    {
        AgregarCatalogoBase();
        var mensaje = AgregarMensajeConError("moneda no soportada", "1000 uno");
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(1000.00m, TipoMovimiento.Egreso, "Hogar", "ARS"));
        // El peor escenario (FR-017b): el guardado del movimiento ya volcó TieneError = false a la
        // base (DbContext compartido) y recién después falla el guardado de Procesado. Si la
        // restauración del error se quedara solo en memoria, el mensaje desaparecería de la
        // bandeja Y tampoco figuraría como pendiente: invisible en toda la app.
        _movimientos.AlGuardarCambios = _mensajes.ConfirmarPersistencia;
        _mensajes.ErrorAlGuardar = new InvalidOperationException("la base no está disponible");

        var resultado = await _servicio.ReprocesarTodosAsync();

        Assert.Equal(1, resultado.Total);
        Assert.Equal(0, resultado.Exitosos);
        Assert.Equal(1, resultado.ConError);
        Assert.True(mensaje.TieneError);
        // Lo que importa: una pantalla que vuelve a consultar la base lo sigue viendo en error.
        var restantes = await _servicio.ListarAsync();
        Assert.Same(mensaje, Assert.Single(restantes));
        Assert.Equal("moneda no soportada", restantes[0].MotivoError);
    }

    [Fact]
    public async Task Reprocesar_todos_con_la_bandeja_vacia_no_explota_y_devuelve_ceros()
    {
        AgregarCatalogoBase();

        var resultado = await _servicio.ReprocesarTodosAsync();

        Assert.Equal(0, resultado.Total);
        Assert.Equal(0, resultado.Exitosos);
        Assert.Equal(0, resultado.ConError);
        Assert.False(_clasificador.FueInvocado);
    }
}
