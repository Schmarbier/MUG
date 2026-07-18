using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Falsos;

public sealed class ClasificadorDeMensajesFalso : IClasificadorDeMensajes
{
    public ResultadoClasificacion Resultado { get; set; } =
        new ResultadoClasificacion.Fallida(new Falla(MotivoFalla.ClasificadorNoDisponible));

    public bool FueInvocado { get; private set; }

    public Task<ResultadoClasificacion> ClasificarAsync(
        string texto,
        IReadOnlyList<CategoriaActiva> categoriasActivas,
        IReadOnlyList<MonedaActiva> monedasActivas,
        CancellationToken ct = default)
    {
        FueInvocado = true;
        return Task.FromResult(Resultado);
    }
}
