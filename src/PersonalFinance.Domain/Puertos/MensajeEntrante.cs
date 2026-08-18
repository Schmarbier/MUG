namespace PersonalFinance.Domain.Puertos;

/// <summary>
/// Un mensaje tal como lo entrega la fuente, antes de ser filtrado por chat autorizado (FR-02)
/// y deduplicado por <see cref="MessageId"/> (FR-04). Todavía no es una entidad: es input no
/// confiable.
/// </summary>
public sealed record MensajeEntrante(long ChatId, long MessageId, string Texto);
