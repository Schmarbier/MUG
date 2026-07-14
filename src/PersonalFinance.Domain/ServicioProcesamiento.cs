namespace PersonalFinance.Domain;

// Orquesta la clasificación de los mensajes pendientes: por cada uno, clasifica y persiste el
// movimiento marcando procesado (RF-10), o marca el mensaje con error y motivo (RF-11).
public class ServicioProcesamiento
{
    private readonly ServicioClasificacion _clasificacion;
    private readonly IRepositorioMensajes _mensajes;
    private readonly IRepositorioMovimientos _movimientos;

    public ServicioProcesamiento(
        ServicioClasificacion clasificacion, IRepositorioMensajes mensajes, IRepositorioMovimientos movimientos)
    {
        _clasificacion = clasificacion;
        _mensajes = mensajes;
        _movimientos = movimientos;
    }

    public async Task<int> ProcesarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _mensajes.ObtenerNoProcesadosAsync(ct);

        foreach (var mensaje in pendientes)
        {
            var resultado = await _clasificacion.ClasificarAsync(mensaje, ct);

            if (resultado.EsExito)
            {
                await _movimientos.AgregarAsync(resultado.Movimiento!, ct);
                mensaje.Procesado = true;
                mensaje.Error = false;
                mensaje.MotivoError = null;
            }
            else
            {
                mensaje.Error = true;
                mensaje.MotivoError = resultado.MotivoError;
            }

            await _mensajes.GuardarCambiosAsync(ct);
        }

        return pendientes.Count;
    }
}
