using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Domain.Tests.Dobles;

/// <summary>
/// Doble de <see cref="IReloj"/>. El puerto existe justamente para esto: que la fecha de un
/// test sea un dato del test y no del momento en que corre.
/// </summary>
internal sealed class RelojFijo : IReloj
{
    public static readonly DateTime Momento = new(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);

    public DateTime UtcNow => Momento;
}
