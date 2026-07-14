namespace PersonalFinance.Domain;

public interface IRepositorioMonedas
{
    Task<Moneda?> ObtenerAsync(string codigo, CancellationToken ct = default);
}
