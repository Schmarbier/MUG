namespace PersonalFinance.Domain;

public interface IRepositorioCategorias
{
    // Solo activas: las desactivadas no participan de la clasificación (RF-23, AC-24).
    Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(CancellationToken ct = default);
}
