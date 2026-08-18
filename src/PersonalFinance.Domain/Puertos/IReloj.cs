namespace PersonalFinance.Domain.Puertos;

/// <summary>
/// Puerto del tiempo. El dominio no llama al reloj del sistema: lo recibe por acá, así las fechas
/// son controlables desde un test.
/// </summary>
public interface IReloj
{
    DateTime UtcNow { get; }
}
