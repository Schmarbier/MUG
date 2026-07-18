using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Web.Tests.Falsos;

public sealed class ClasificadorDeMensajesFalso : IClasificadorDeMensajes
{
    public ResultadoClasificacion Resultado { get; set; } =
        new ResultadoClasificacion.Fallida(new Falla(MotivoFalla.ClasificadorNoDisponible));

    public Task<ResultadoClasificacion> ClasificarAsync(
        string texto,
        IReadOnlyList<CategoriaActiva> categoriasActivas,
        IReadOnlyList<MonedaActiva> monedasActivas,
        CancellationToken ct = default) => Task.FromResult(Resultado);
}
