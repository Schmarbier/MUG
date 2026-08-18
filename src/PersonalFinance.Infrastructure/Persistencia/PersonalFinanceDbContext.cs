using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Infrastructure.Persistencia;

/// <summary>
/// Contexto de EF Core sobre SQLite. Vive únicamente en Infrastructure (AGENTS.md ->
/// Architecture conventions): ni Domain ni los composition roots lo conocen.
/// </summary>
public sealed class PersonalFinanceDbContext : DbContext
{
    public PersonalFinanceDbContext(DbContextOptions<PersonalFinanceDbContext> opciones)
        : base(opciones)
    {
    }

    public DbSet<Mensaje> Mensajes => Set<Mensaje>();

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // El mapeo va exclusivamente por IEntityTypeConfiguration<T>: ninguna entidad de Domain
        // lleva atributos de EF Core.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PersonalFinanceDbContext).Assembly);
    }
}
