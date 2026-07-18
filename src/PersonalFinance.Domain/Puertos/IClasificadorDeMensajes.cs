using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Puertos;

/// <summary>
/// Frontera del Principio II: el dominio solo ve este contrato, nunca un prompt, un
/// cliente HTTP ni JSON crudo. Ver contracts/clasificador.md.
/// </summary>
public interface IClasificadorDeMensajes
{
    Task<ResultadoClasificacion> ClasificarAsync(
        string texto,
        IReadOnlyList<CategoriaActiva> categoriasActivas,
        IReadOnlyList<MonedaActiva> monedasActivas,
        CancellationToken ct = default);
}

public record CategoriaActiva(string Titulo, string Descripcion);

public record MonedaActiva(string Codigo, bool EsBase);

/// <summary>Exactamente uno de dos resultados; no hay tercer camino (Principio III).</summary>
public abstract record ResultadoClasificacion
{
    private ResultadoClasificacion()
    {
    }

    public sealed record Exitosa(Clasificacion Clasificacion) : ResultadoClasificacion;

    public sealed record Fallida(Falla Falla) : ResultadoClasificacion;
}

/// <summary>
/// CodigoMoneda es nulo cuando el mensaje no la especifica; el dominio asume la base (FR-008).
/// </summary>
public record Clasificacion(decimal Monto, TipoMovimiento Tipo, string TituloCategoria, string? CodigoMoneda);

public record Falla(MotivoFalla Motivo);

public enum MotivoFalla
{
    SinMonto,
    SinDescripcion,
    MonedaNoSoportada,
    SinConfianza,
    ClasificadorNoDisponible
}
