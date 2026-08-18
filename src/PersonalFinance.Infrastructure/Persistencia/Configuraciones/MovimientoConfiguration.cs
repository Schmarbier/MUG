using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Infrastructure.Persistencia.Configuraciones;

public sealed class MovimientoConfiguration : IEntityTypeConfiguration<Movimiento>
{
    public void Configure(EntityTypeBuilder<Movimiento> builder)
    {
        builder.ToTable("Movimiento");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.MensajeId).IsRequired();
        builder.Property(m => m.CategoriaId).IsRequired();

        builder.Property(m => m.Monto).IsRequired().HasPrecision(18, 2);

        // El enum se persiste por su valor entero, como declara el data model del spec.
        builder.Property(m => m.Tipo).IsRequired().HasConversion<int>();

        builder.Property(m => m.FechaCreacion).IsRequired();

        // Un Mensaje produce como máximo un Movimiento.
        builder.HasIndex(m => m.MensajeId)
            .IsUnique()
            .HasDatabaseName("IX_Movimiento_MensajeId");

        // Sin propiedades de navegación: las entidades de Domain no las declaran, así que las
        // relaciones se configuran acá por su clave foránea.
        builder.HasOne<Mensaje>()
            .WithOne()
            .HasForeignKey<Movimiento>(m => m.MensajeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(m => m.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
