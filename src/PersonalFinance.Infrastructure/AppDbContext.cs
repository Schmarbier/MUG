using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Moneda> Monedas => Set<Moneda>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Moneda>(e =>
        {
            e.HasKey(m => m.Codigo);
            e.Property(m => m.Codigo).HasMaxLength(3);
            e.Property(m => m.TipoCambio).HasPrecision(18, 6);
            // RF-24: ARS preexistente como moneda base, sin que el usuario la cargue.
            e.HasData(new Moneda { Codigo = "ARS", TipoCambio = 1m, EsBase = true });
        });

        model.Entity<Categoria>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Titulo).IsRequired().HasMaxLength(60);
            // RF-05 / AC-17: Titulo único.
            e.HasIndex(c => c.Titulo).IsUnique();
            // Seed inicial: categorías fijas para que la clasificación tenga dónde asignar.
            e.HasData(
                new Categoria { Id = 1, Titulo = "Hogar", Descripcion = "Gastos del hogar", Activa = true },
                new Categoria { Id = 2, Titulo = "Sueldo", Descripcion = "Ingresos por sueldo", Activa = true },
                new Categoria { Id = 3, Titulo = "Ahorro", Descripcion = "Ahorros e inversiones", Activa = true },
                new Categoria { Id = 4, Titulo = "Ocio", Descripcion = "Entretenimiento y tiempo libre", Activa = true });
        });

        model.Entity<Mensaje>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Texto).IsRequired();
            // RF-04: dedup por message_id de Telegram.
            e.HasIndex(m => m.MessageId).IsUnique();
        });

        model.Entity<Movimiento>(e =>
        {
            e.HasKey(mv => mv.Id);
            e.Property(mv => mv.Monto).HasPrecision(18, 2);
            e.Property(mv => mv.TipoCambioHistorico).HasPrecision(18, 6);
            e.Property(mv => mv.MonedaCodigo).HasMaxLength(3);
            e.HasOne(mv => mv.Mensaje).WithMany().HasForeignKey(mv => mv.MensajeId);
            e.HasOne(mv => mv.Categoria).WithMany().HasForeignKey(mv => mv.CategoriaId);
            e.HasOne(mv => mv.Moneda).WithMany().HasForeignKey(mv => mv.MonedaCodigo);
        });
    }
}
