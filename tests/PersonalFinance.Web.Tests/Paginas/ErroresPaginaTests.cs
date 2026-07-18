using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Web.Tests.Falsos;
using PaginaErrores = PersonalFinance.Web.Components.Pages.Errores;

namespace PersonalFinance.Web.Tests.Paginas;

public sealed class ErroresPaginaTests : BunitContext
{
    private readonly RepositorioMensajeFalso _mensajes = new();
    private readonly RepositorioCategoriaFalso _categorias = new();
    private readonly RepositorioMonedaFalso _monedas = new();
    private readonly RepositorioMovimientoFalso _movimientos = new();
    private readonly ClasificadorDeMensajesFalso _clasificador = new();

    public ErroresPaginaTests()
    {
        var clasificacionServicio = new ClasificacionServicio(_clasificador, _categorias, _monedas, _movimientos, _mensajes);
        Services.AddSingleton(new BandejaErroresServicio(_mensajes, clasificacionServicio));
    }

    private Mensaje AgregarMensajeConError(string motivo)
    {
        var mensaje = new Mensaje
        {
            Id = _mensajes.Mensajes.Count + 1,
            IdentificadorCanal = _mensajes.Mensajes.Count + 1,
            Texto = "2000 en EUR viaje",
            FechaRecepcionUtc = DateTimeOffset.UtcNow,
            Procesado = false,
            IntentosClasificacion = 0,
            TieneError = true,
            MotivoError = motivo
        };
        _mensajes.Mensajes.Add(mensaje);
        return mensaje;
    }

    [Fact]
    public void Lista_los_mensajes_con_error_y_su_motivo()
    {
        AgregarMensajeConError("moneda no soportada");

        var componente = Render<PaginaErrores>(
            (ComponentParameterCollectionBuilder<PaginaErrores> parametros) => { });

        Assert.Contains("2000 en EUR viaje", componente.Markup);
        Assert.Contains("moneda no soportada", componente.Markup);
    }

    [Fact]
    public void Reprocesar_un_mensaje_corregido_lo_quita_del_listado_de_errores()
    {
        var categoria = new Categoria { Id = 1, Titulo = "Hogar", Descripcion = "d", Activa = true };
        _categorias.Categorias.Add(categoria);
        var ars = new Moneda { Id = 1, Codigo = "ARS", EsBase = true, Activa = true, TipoDeCambio = null };
        _monedas.Monedas.Add(ars);
        AgregarMensajeConError("moneda no soportada");
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(2000.00m, TipoMovimiento.Egreso, "Hogar", "ARS"));

        var componente = Render<PaginaErrores>(
            (ComponentParameterCollectionBuilder<PaginaErrores> parametros) => { });

        componente.Find("button:contains('Reprocesar')").Click();

        Assert.DoesNotContain("2000 en EUR viaje", componente.Markup);
    }
}
