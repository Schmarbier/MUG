using Microsoft.EntityFrameworkCore;
using PersonalFinance.Bot.Domain;

namespace PersonalFinance.Bot.Data;

public class PersonalFinanceContext : DbContext
{
    public const string DefaultDbFileName = "personalfinance.db";

    public PersonalFinanceContext(DbContextOptions<PersonalFinanceContext> options)
        : base(options)
    {
    }

    // Constructor sin parámetros: usado en tiempo de diseño (migraciones) y como default runtime.
    public PersonalFinanceContext()
    {
    }

    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={DefaultDbFileName}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mensaje>(e =>
        {
            // Deduplicación de la ingesta: no dos mensajes con el mismo MessageId de Telegram.
            e.HasIndex(m => m.MessageId).IsUnique();
        });

        modelBuilder.Entity<Movimiento>(e =>
        {
            e.Property(m => m.Monto).HasColumnType("TEXT"); // decimal exacto en SQLite (no REAL).

            e.HasOne(m => m.Mensaje)
                .WithOne(msg => msg.Movimiento)
                .HasForeignKey<Movimiento>(m => m.MensajeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Categoria)
                .WithMany(c => c.Movimientos)
                .HasForeignKey(m => m.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed fijo de categorías (RF-03): existen desde el arranque para poder clasificar.
        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Titulo = "Sueldo", Descripcion = "ingresos por sueldo o salario" },
            new Categoria { Id = 2, Titulo = "Hogar", Descripcion = "gastos del hogar" },
            new Categoria { Id = 3, Titulo = "Comida", Descripcion = "comida y supermercado" },
            new Categoria { Id = 4, Titulo = "Transporte", Descripcion = "transporte y combustible" },
            new Categoria { Id = 5, Titulo = "Ocio", Descripcion = "entretenimiento y salidas" },
            new Categoria { Id = 6, Titulo = "Otros", Descripcion = "movimientos sin categoría específica" }
        );
    }
}
