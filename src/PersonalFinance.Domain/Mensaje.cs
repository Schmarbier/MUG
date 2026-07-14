namespace PersonalFinance.Domain;

public class Mensaje
{
    public int Id { get; set; }

    // Identificador de Telegram; clave de deduplicación (RF-04).
    public long MessageId { get; set; }
    public long ChatId { get; set; }

    public string Texto { get; set; } = string.Empty;
    public DateTime FechaMensaje { get; set; }

    public bool Procesado { get; set; }
    public bool Error { get; set; }
    public string? MotivoError { get; set; }
}
