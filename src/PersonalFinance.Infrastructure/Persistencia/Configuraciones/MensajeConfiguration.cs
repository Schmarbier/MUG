using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Infrastructure.Persistencia.Configuraciones;

public sealed class MensajeConfiguration : IEntityTypeConfiguration<Mensaje>
{
    public void Configure(EntityTypeBuilder<Mensaje> builder)
    {
        builder.ToTable("Mensaje");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.MessageId).IsRequired();

        builder.Property(m => m.Texto)
            .IsRequired()
            .HasMaxLength(Mensaje.TextoMaximo);

        builder.Property(m => m.FechaRecepcion).IsRequired();

        builder.Property(m => m.Procesado).IsRequired().HasDefaultValue(false);
        builder.Property(m => m.Error).IsRequired().HasDefaultValue(false);

        builder.Property(m => m.Motivo).HasMaxLength(Mensaje.MotivoMaximo);

        // Clave de deduplicación de FR-04.
        builder.HasIndex(m => m.MessageId)
            .IsUnique()
            .HasDatabaseName("IX_Mensaje_MessageId");

        // La consulta que ejecuta ClasificarMensajesPendientes en cada corrida.
        builder.HasIndex(m => new { m.Procesado, m.Error })
            .HasDatabaseName("IX_Mensaje_Procesado_Error");
    }
}
