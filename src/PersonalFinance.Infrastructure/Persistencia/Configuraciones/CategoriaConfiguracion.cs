using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Infrastructure.Persistencia.Configuraciones;

public class CategoriaConfiguracion : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Titulo).IsRequired();
        builder.Property(c => c.Descripcion).IsRequired();

        // Único entre todas las categorías, incluidas las desactivadas (FR-024, FR-026).
        builder.HasIndex(c => c.Titulo).IsUnique();
    }
}
