namespace PersonalFinance.Domain.Entidades;

public class Mensaje
{
    public int Id { get; set; }

    /// <summary>message_id de Telegram; único (FR-004, R4).</summary>
    public long IdentificadorCanal { get; set; }

    public required string Texto { get; set; }
    public DateTimeOffset FechaRecepcionUtc { get; set; }
    public bool Procesado { get; set; }

    /// <summary>Tope de reintentos ante fallo del clasificador (FR-010a).</summary>
    public int IntentosClasificacion { get; set; }

    public bool TieneError { get; set; }
    public string? MotivoError { get; set; }
}
