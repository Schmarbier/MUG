using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Falsos;

public sealed class ClasificadorDeMensajesFalso : IClasificadorDeMensajes
{
    public ResultadoClasificacion Resultado { get; set; } =
        new ResultadoClasificacion.Fallida(new Falla(MotivoFalla.ClasificadorNoDisponible));

    /// <summary>
    /// Permite que un lote de mensajes tenga resultados distintos según su texto (por ejemplo,
    /// un reproceso masivo donde algunos se resuelven y otros siguen fallando). Si el texto no
    /// está en el mapa se usa <see cref="Resultado"/>.
    /// </summary>
    public Dictionary<string, ResultadoClasificacion> ResultadoPorTexto { get; } = [];

    public bool FueInvocado { get; private set; }

    public Task<ResultadoClasificacion> ClasificarAsync(
        string texto,
        IReadOnlyList<CategoriaActiva> categoriasActivas,
        IReadOnlyList<MonedaActiva> monedasActivas,
        CancellationToken ct = default)
    {
        FueInvocado = true;
        return Task.FromResult(
            ResultadoPorTexto.TryGetValue(texto, out var especifico) ? especifico : Resultado);
    }
}
