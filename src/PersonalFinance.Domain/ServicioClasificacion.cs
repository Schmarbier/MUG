namespace PersonalFinance.Domain;

// Reglas determinísticas de clasificación (RF-06 a RF-09, RF-11). Toma la extracción del
// agente y decide: crea un movimiento válido o devuelve el motivo de error. No persiste nada
// ni muta el mensaje: eso lo hace el orquestador (ServicioProcesamiento).
public class ServicioClasificacion
{
    private readonly IAgenteClasificador _agente;
    private readonly IRepositorioCategorias _categorias;
    private readonly IRepositorioMonedas _monedas;

    public ServicioClasificacion(
        IAgenteClasificador agente, IRepositorioCategorias categorias, IRepositorioMonedas monedas)
    {
        _agente = agente;
        _categorias = categorias;
        _monedas = monedas;
    }

    public async Task<ResultadoClasificacion> ClasificarAsync(Mensaje mensaje, CancellationToken ct = default)
    {
        var categoriasActivas = await _categorias.ObtenerActivasAsync(ct);
        var ext = await _agente.ExtraerAsync(mensaje.Texto, categoriasActivas, ct);

        if (ext.Monto is not { } monto || monto <= 0)
            return ResultadoClasificacion.ConError("no contiene monto");

        if (string.IsNullOrWhiteSpace(ext.Descripcion))
            return ResultadoClasificacion.ConError("no contiene descripcion");

        // Moneda: sin código explícito => ARS base (RF-09), la base no lleva tipo de cambio
        // histórico. Con código, debe estar cargada (RF-11/AC-30) y se congela su cotización (RF-27).
        string monedaCodigo;
        decimal? tipoCambioHistorico;
        if (string.IsNullOrWhiteSpace(ext.MonedaCodigo))
        {
            monedaCodigo = "ARS";
            tipoCambioHistorico = null;
        }
        else
        {
            var moneda = await _monedas.ObtenerAsync(ext.MonedaCodigo, ct);
            if (moneda is null)
                return ResultadoClasificacion.ConError("moneda no soportada");

            monedaCodigo = moneda.Codigo;
            tipoCambioHistorico = moneda.EsBase ? null : moneda.TipoCambio;
        }

        var categoria = categoriasActivas.FirstOrDefault(c =>
            string.Equals(c.Titulo, ext.CategoriaTitulo, StringComparison.OrdinalIgnoreCase));
        if (categoria is null)
            return ResultadoClasificacion.ConError("categoria no reconocida");

        var movimiento = new Movimiento
        {
            MensajeId = mensaje.Id,
            CategoriaId = categoria.Id,
            Tipo = ext.Tipo,
            Monto = monto,
            MonedaCodigo = monedaCodigo,
            TipoCambioHistorico = tipoCambioHistorico,
            Fecha = mensaje.FechaMensaje,
        };
        return ResultadoClasificacion.Exitoso(movimiento);
    }
}
