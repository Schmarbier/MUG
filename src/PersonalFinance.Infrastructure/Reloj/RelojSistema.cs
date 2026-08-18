using PersonalFinance.Domain.Puertos;

namespace PersonalFinance.Infrastructure.Reloj;

/// <summary>
/// Adaptador de <see cref="IReloj"/> contra el reloj del sistema. Existe para que Domain no
/// llame a <see cref="DateTime.UtcNow"/> directo (AGENTS.md -> Code conventions).
/// </summary>
public sealed class RelojSistema : IReloj
{
    public DateTime UtcNow => DateTime.UtcNow;
}
