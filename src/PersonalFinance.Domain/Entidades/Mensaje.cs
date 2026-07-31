namespace PersonalFinance.Domain.Entidades;

/// <summary>
/// Lo que llega por Telegram y se guarda tal cual, con su estado de procesamiento.
/// </summary>
public class Mensaje
{
    /// <summary>Largo máximo del texto original, igual al límite de un mensaje de Telegram.</summary>
    public const int TextoMaximo = 4096;

    /// <summary>Largo máximo del motivo por el que el mensaje no pudo convertirse.</summary>
    public const int MotivoMaximo = 200;

    public Mensaje(long messageId, string texto, DateTime fechaRecepcion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(texto.Length, TextoMaximo, nameof(texto));

        MessageId = messageId;
        Texto = texto;
        FechaRecepcion = fechaRecepcion;
    }

    public long Id { get; private set; }

    public long MessageId { get; private set; }

    public string Texto { get; private set; }

    public DateTime FechaRecepcion { get; private set; }

    public bool Procesado { get; private set; }

    public bool Error { get; private set; }

    public string? Motivo { get; private set; }

    public void MarcarProcesado()
    {
        if (Error)
        {
            throw new InvalidOperationException(
                $"El mensaje {MessageId} está marcado con error y no puede marcarse como procesado.");
        }

        Procesado = true;
    }

    public void MarcarError(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(motivo.Length, MotivoMaximo, nameof(motivo));

        Error = true;
        Motivo = motivo;
    }
}
