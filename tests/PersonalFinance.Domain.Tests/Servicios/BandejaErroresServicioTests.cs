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

    private Mensaje AgregarMensajeConError(string motivo)
    {
        var mensaje = new Mensaje
        {
            Id = _mensajes.Mensajes.Count + 1,
            IdentificadorCanal = _mensajes.Mensajes.Count + 1,
            Texto = "texto",
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
}
