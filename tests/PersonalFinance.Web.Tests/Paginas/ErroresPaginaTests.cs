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

    private Mensaje AgregarMensajeConError(string motivo, string texto = "2000 en EUR viaje")
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

        // "td button" a propósito: el botón de reproceso masivo también matchea 'Reprocesar'.
        componente.Find("td button:contains('Reprocesar')").Click();

        Assert.DoesNotContain("2000 en EUR viaje", componente.Markup);
    }

    private void AgregarCatalogoBase()
    {
        _categorias.Categorias.Add(new Categoria { Id = 1, Titulo = "Hogar", Descripcion = "d", Activa = true });
        _monedas.Monedas.Add(new Moneda { Id = 1, Codigo = "ARS", EsBase = true, Activa = true, TipoDeCambio = null });
    }

    [Fact]
    public void Reprocesar_todos_vacia_la_bandeja_e_informa_el_resultado()
    {
        AgregarCatalogoBase();
        AgregarMensajeConError("moneda no soportada", "1000 uno");
        AgregarMensajeConError("moneda no soportada", "1000 dos");
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(1000.00m, TipoMovimiento.Egreso, "Hogar", "ARS"));

        var componente = Render<PaginaErrores>(
            (ComponentParameterCollectionBuilder<PaginaErrores> parametros) => { });

        componente.Find("button:contains('Reprocesar todos')").Click();

        Assert.Contains("2 de 2 reprocesados correctamente.", componente.Markup);
        Assert.DoesNotContain("1000 uno", componente.Markup);
        Assert.DoesNotContain("1000 dos", componente.Markup);
    }

    [Fact]
    public void Reprocesar_todos_informa_los_que_siguen_en_error_y_los_deja_en_la_lista()
    {
        AgregarCatalogoBase();
        AgregarMensajeConError("moneda no soportada", "1000 bueno");
        AgregarMensajeConError("no contiene monto", "sin monto");
        _clasificador.Resultado = new ResultadoClasificacion.Exitosa(
            new Clasificacion(1000.00m, TipoMovimiento.Egreso, "Hogar", "ARS"));
        _clasificador.ResultadoPorTexto["sin monto"] =
            new ResultadoClasificacion.Fallida(new Falla(MotivoFalla.SinMonto));

        var componente = Render<PaginaErrores>(
            (ComponentParameterCollectionBuilder<PaginaErrores> parametros) => { });

        componente.Find("button:contains('Reprocesar todos')").Click();

        Assert.Contains("1 de 2 reprocesados correctamente.", componente.Markup);
        Assert.DoesNotContain("1000 bueno", componente.Markup);
        Assert.Contains("sin monto", componente.Markup);
    }

    [Fact]
    public void Sin_mensajes_en_error_no_se_ofrece_reprocesar_todos()
    {
        var componente = Render<PaginaErrores>(
            (ComponentParameterCollectionBuilder<PaginaErrores> parametros) => { });

        Assert.Empty(componente.FindAll("button:contains('Reprocesar todos')"));
    }
}
