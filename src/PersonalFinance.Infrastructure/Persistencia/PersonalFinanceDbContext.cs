using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Infrastructure.Persistencia;

public class PersonalFinanceDbContext(DbContextOptions<PersonalFinanceDbContext> opciones)
    : DbContext(opciones)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Moneda> Monedas => Set<Moneda>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PersonalFinanceDbContext).Assembly);
    }
}
