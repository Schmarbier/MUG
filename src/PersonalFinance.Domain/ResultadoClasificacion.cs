namespace PersonalFinance.Domain;

public sealed class ResultadoClasificacion
{
    public bool EsExito { get; }
    public Movimiento? Movimiento { get; }
    public string? MotivoError { get; }

    private ResultadoClasificacion(bool exito, Movimiento? movimiento, string? motivoError)
    {
        EsExito = exito;
        Movimiento = movimiento;
        MotivoError = motivoError;
    }

    public static ResultadoClasificacion Exitoso(Movimiento movimiento) => new(true, movimiento, null);
    public static ResultadoClasificacion ConError(string motivo) => new(false, null, motivo);
}
