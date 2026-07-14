namespace PersonalFinance.Domain;

// Lo que el adaptador de Telegram entrega al núcleo, ya desacoplado del SDK.
public record MensajeEntrante(long MessageId, long ChatId, string Texto, DateTime Fecha);
