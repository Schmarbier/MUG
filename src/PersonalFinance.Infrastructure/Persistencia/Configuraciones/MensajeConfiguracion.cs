using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Infrastructure.Persistencia.Configuraciones;

public class MensajeConfiguracion : IEntityTypeConfiguration<Mensaje>
{
    public void Configure(EntityTypeBuilder<Mensaje> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Texto).IsRequired();

        // Garantía real frente a la carrera entre ingesta por polling y barrido (FR-004, R4).
        builder.HasIndex(m => m.IdentificadorCanal).IsUnique();
    }
}
