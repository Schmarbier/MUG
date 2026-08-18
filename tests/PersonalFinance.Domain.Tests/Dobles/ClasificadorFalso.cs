using PersonalFinance.Domain.Clasificacion;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Dobles;

/// <summary>
/// Doble de <see cref="IClasificador"/>: devuelve resultados preparados, en orden. El último se
/// repite si hay más mensajes que resultados.
/// </summary>
internal sealed class ClasificadorFalso : IClasificador
{
    private readonly IReadOnlyList<ResultadoClasificacion> _resultados;

    private ClasificadorFalso(IReadOnlyList<ResultadoClasificacion> resultados) => _resultados = resultados;

    public int Llamadas { get; private set; }

    public IReadOnlyList<Categoria>? UltimasCategoriasRecibidas { get; private set; }

    public static ClasificadorFalso Con(params ResultadoClasificacion[] resultados) => new(resultados);

    public Task<ResultadoClasificacion> ClasificarAsync(
        string texto,
        IReadOnlyList<Categoria> categoriasActivas,
        CancellationToken cancellationToken)
    {
        UltimasCategoriasRecibidas = categoriasActivas;
        var resultado = _resultados[Math.Min(Llamadas, _resultados.Count - 1)];
        Llamadas++;

        return Task.FromResult(resultado);
    }
}
