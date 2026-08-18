using PersonalFinance.Domain.Clasificacion;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.CasosDeUso;

/// <summary>
/// Toma los mensajes sin procesar y los convierte en movimientos (FR-06 a FR-08, FR-10 a FR-12).
/// </summary>
public sealed class ClasificarMensajesPendientes
{
    /// <summary>Motivos del PRD. Son texto de negocio, no mensajes de excepción.</summary>
    public const string MotivoSinMonto = "no contiene monto";

    public const string MotivoSinDescripcion = "no contiene descripcion";

    public const string MotivoTipoNoReconocido = "tipo no reconocido";

    private readonly IRepositorioMensajes _mensajes;
    private readonly IRepositorioCategorias _categorias;
    private readonly IRepositorioMovimientos _movimientos;
    private readonly IClasificador _clasificador;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReloj _reloj;

    public ClasificarMensajesPendientes(
        IRepositorioMensajes mensajes,
        IRepositorioCategorias categorias,
        IRepositorioMovimientos movimientos,
        IClasificador clasificador,
        IUnitOfWork unitOfWork,
        IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(mensajes);
        ArgumentNullException.ThrowIfNull(categorias);
        ArgumentNullException.ThrowIfNull(movimientos);
        ArgumentNullException.ThrowIfNull(clasificador);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(reloj);

        _mensajes = mensajes;
        _categorias = categorias;
        _movimientos = movimientos;
        _clasificador = clasificador;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    public async Task<Resultado> EjecutarAsync(CancellationToken cancellationToken)
    {
        var pendientes = await _mensajes.ObtenerPendientesAsync(cancellationToken);

        if (pendientes.Count == 0)
        {
            return new Resultado(Clasificados: 0, ConError: 0, NoDisponibles: 0, Abortada: false);
        }

        var activas = await _categorias.ObtenerActivasAsync(cancellationToken);

        if (activas.Count == 0)
        {
            // Sin categorías no hay clasificación posible, y marcar error en todos sería
            // destruir datos recuperables. Se aborta sin tocar ningún mensaje.
            return new Resultado(Clasificados: 0, ConError: 0, NoDisponibles: 0, Abortada: true);
        }

        var clasificados = 0;
        var conError = 0;
        var noDisponibles = 0;

        foreach (var mensaje in pendientes)
        {
            var resultado = await _clasificador.ClasificarAsync(mensaje.Texto, activas, cancellationToken);

            if (resultado is ResultadoClasificacion.NoDisponible)
            {
                // FR-12: falla de infraestructura, no dato malo. El mensaje queda intacto y lo
                // levanta la próxima corrida.
                noDisponibles++;
                continue;
            }

            if (resultado is ResultadoClasificacion.Clasificado clasificado)
            {
                await _movimientos.AgregarAsync(
                    Movimiento.Crear(
                        mensaje.Id,
                        clasificado.Categoria.Id,
                        clasificado.Monto,
                        clasificado.Tipo,
                        _reloj.UtcNow),
                    cancellationToken);

                mensaje.MarcarProcesado();   // FR-10
            }
            else
            {
                mensaje.MarcarError(MotivoDe(resultado));   // FR-11
            }

            try
            {
                // Una confirmación por mensaje: el movimiento y el nuevo estado del mensaje se
                // guardan juntos, o no se guarda ninguno.
                await _unitOfWork.ConfirmarAsync(cancellationToken);
            }
            catch (Exception excepcion) when (excepcion is not OperationCanceledException)
            {
                // No se confirmó: no quedó ni movimiento ni cambio de estado. Se corta la
                // corrida en vez de seguir con el próximo mensaje, porque la unidad de trabajo
                // quedó con cambios pendientes que la próxima confirmación arrastraría,
                // rompiendo justamente el "o los dos, o ninguno". La próxima corrida arranca
                // limpia y estos mensajes siguen pendientes.
                return new Resultado(clasificados, conError, noDisponibles, Abortada: true);
            }

            if (resultado is ResultadoClasificacion.Clasificado)
            {
                clasificados++;
            }
            else
            {
                conError++;
            }
        }

        return new Resultado(clasificados, conError, noDisponibles, Abortada: false);
    }

    private static string MotivoDe(ResultadoClasificacion resultado) => resultado switch
    {
        ResultadoClasificacion.SinMonto => MotivoSinMonto,
        ResultadoClasificacion.SinDescripcion => MotivoSinDescripcion,
        ResultadoClasificacion.TipoNoReconocido => MotivoTipoNoReconocido,
        _ => throw new ArgumentOutOfRangeException(
            nameof(resultado), resultado, "Resultado de clasificación sin motivo asociado."),
    };

    /// <summary>
    /// Resumen de la corrida. Son cantidades: M-03 prohíbe que el texto de un mensaje llegue a
    /// un log.
    /// </summary>
    public sealed record Resultado(int Clasificados, int ConError, int NoDisponibles, bool Abortada);
}
