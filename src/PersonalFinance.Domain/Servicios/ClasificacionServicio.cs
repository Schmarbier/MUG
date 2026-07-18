using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Servicios;

/// <summary>
/// Traduce el resultado del puerto de IA en un Movimiento o en un motivo de error
/// persistido (Principio III: ninguna salida del modelo se persiste sin validar).
/// </summary>
public class ClasificacionServicio(
    IClasificadorDeMensajes clasificador,
    ICategoriaRepositorio categoriaRepositorio,
    IMonedaRepositorio monedaRepositorio,
    IMovimientoRepositorio movimientoRepositorio,
    IMensajeRepositorio mensajeRepositorio)
{
    private const int MaximoIntentos = 3;

    public async Task ClasificarAsync(Mensaje mensaje, CancellationToken ct = default)
    {
        var categoriasActivas = await categoriaRepositorio.ListarActivasAsync(ct);
        if (categoriasActivas.Count == 0)
        {
            await MarcarErrorAsync(mensaje, "no hay categorías disponibles para clasificar", ct);
            return;
        }

        var monedasActivas = await monedaRepositorio.ListarActivasAsync(ct);

        var resultado = await clasificador.ClasificarAsync(
            mensaje.Texto,
            [.. categoriasActivas.Select(c => new CategoriaActiva(c.Titulo, c.Descripcion))],
            [.. monedasActivas.Select(m => new MonedaActiva(m.Codigo, m.EsBase))],
            ct);

        switch (resultado)
        {
            case ResultadoClasificacion.Exitosa exitosa:
                await CrearMovimientoAsync(mensaje, exitosa.Clasificacion, categoriasActivas, monedasActivas, ct);
                break;

            case ResultadoClasificacion.Fallida { Falla.Motivo: MotivoFalla.ClasificadorNoDisponible }:
                await RegistrarFallaDeClasificadorAsync(mensaje, ct);
                break;

            case ResultadoClasificacion.Fallida fallida:
                await MarcarErrorAsync(mensaje, MapearMotivo(fallida.Falla.Motivo), ct);
                break;
        }
    }

    private async Task CrearMovimientoAsync(
        Mensaje mensaje,
        Clasificacion clasificacion,
        IReadOnlyList<Categoria> categoriasActivas,
        IReadOnlyList<Moneda> monedasActivas,
        CancellationToken ct)
    {
        var categoria = categoriasActivas.First(c => c.Titulo == clasificacion.TituloCategoria);
        var moneda = clasificacion.CodigoMoneda is null
            ? monedasActivas.First(m => m.EsBase)
            : monedasActivas.First(m => m.Codigo == clasificacion.CodigoMoneda);

        var movimiento = new Movimiento
        {
            MensajeId = mensaje.Id,
            CategoriaId = categoria.Id,
            MonedaId = moneda.Id,
            Monto = clasificacion.Monto,
            Tipo = clasificacion.Tipo,
            Fecha = ZonaHorariaLocal.DerivarFechaLocal(mensaje.FechaRecepcionUtc),
            TipoDeCambioHistorico = moneda.EsBase ? null : moneda.TipoDeCambio
        };

        await movimientoRepositorio.AgregarAsync(movimiento, ct);
        await movimientoRepositorio.GuardarCambiosAsync(ct);

        mensaje.Procesado = true;
        await mensajeRepositorio.GuardarCambiosAsync(ct);
    }

    private async Task RegistrarFallaDeClasificadorAsync(Mensaje mensaje, CancellationToken ct)
    {
        mensaje.IntentosClasificacion++;

        if (mensaje.IntentosClasificacion >= MaximoIntentos)
        {
            mensaje.TieneError = true;
            mensaje.MotivoError = "clasificador no disponible";
        }

        await mensajeRepositorio.GuardarCambiosAsync(ct);
    }

    private async Task MarcarErrorAsync(Mensaje mensaje, string motivo, CancellationToken ct)
    {
        mensaje.TieneError = true;
        mensaje.MotivoError = motivo;
        await mensajeRepositorio.GuardarCambiosAsync(ct);
    }

    private static string MapearMotivo(MotivoFalla motivo) => motivo switch
    {
        MotivoFalla.SinMonto => "no contiene monto",
        MotivoFalla.SinDescripcion => "no contiene descripción",
        MotivoFalla.MonedaNoSoportada => "moneda no soportada",
        MotivoFalla.SinConfianza => "no se pudo determinar la categoría con confianza",
        MotivoFalla.ClasificadorNoDisponible => "clasificador no disponible",
        _ => throw new ArgumentOutOfRangeException(nameof(motivo))
    };
}
