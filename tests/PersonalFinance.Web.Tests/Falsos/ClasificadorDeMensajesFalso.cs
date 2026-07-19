using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Web.Tests.Falsos;

public sealed class ClasificadorDeMensajesFalso : IClasificadorDeMensajes
{
    public ResultadoClasificacion Resultado { get; set; } =
        new ResultadoClasificacion.Fallida(new Falla(MotivoFalla.ClasificadorNoDisponible));

    /// <summary>
    /// Resultados por texto de mensaje, para lotes donde algunos se resuelven y otros no.
    /// Si el texto no está en el mapa se usa <see cref="Resultado"/>.
    /// </summary>
    public Dictionary<string, ResultadoClasificacion> ResultadoPorTexto { get; } = [];

    public Task<ResultadoClasificacion> ClasificarAsync(
        string texto,
        IReadOnlyList<CategoriaActiva> categoriasActivas,
        IReadOnlyList<MonedaActiva> monedasActivas,
        CancellationToken ct = default) =>
        Task.FromResult(ResultadoPorTexto.TryGetValue(texto, out var especifico) ? especifico : Resultado);
}
