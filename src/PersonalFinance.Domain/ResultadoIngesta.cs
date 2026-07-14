namespace PersonalFinance.Domain;

public enum ResultadoIngesta
{
    Descartado, // chat no autorizado (RF-02)
    Duplicado,  // message_id ya ingerido (RF-04)
    Guardado,   // nuevo mensaje persistido como no procesado (RF-01, RF-03)
}
