namespace PersonalFinance.Bot.Domain;

/// <summary>
/// Mensaje leído de Telegram y guardado (RF-01, RF-02). Nace "no procesado"; luego el
/// procesador lo marca como procesado (RF-06) o con error y motivo (RF-07).
/// </summary>
public class Mensaje
{
    public int Id { get; set; }

    /// <summary>Id del mensaje en Telegram. Único: se usa para deduplicar la ingesta.</summary>
    public required long MessageId { get; set; }

    /// <summary>Texto crudo del mensaje, ej. "$2.000 comida casa".</summary>
    public required string Texto { get; set; }

    /// <summary>Momento en que se ingirió el mensaje.</summary>
    public DateTimeOffset FechaRecibido { get; set; }

    /// <summary>true cuando se creó su movimiento correctamente (RF-06).</summary>
    public bool Procesado { get; set; }

    /// <summary>true si no pudo convertirse en movimiento (RF-07).</summary>
    public bool Error { get; set; }

    /// <summary>Motivo del error, ej. "no contiene monto" (RF-07). null si no hay error.</summary>
    public string? Motivo { get; set; }

    /// <summary>Movimiento creado a partir de este mensaje, si se procesó bien.</summary>
    public Movimiento? Movimiento { get; set; }
}
