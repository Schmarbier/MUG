using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Infrastructure.Persistencia.Configuraciones;

public sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categoria");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Titulo)
            .IsRequired()
            .HasMaxLength(Categoria.TituloMaximo);

        builder.Property(c => c.Descripcion)
            .IsRequired()
            .HasMaxLength(Categoria.DescripcionMaximo);

        builder.Property(c => c.Activa).IsRequired().HasDefaultValue(true);

        builder.HasIndex(c => c.Titulo)
            .IsUnique()
            .HasDatabaseName("IX_Categoria_Titulo");
    }
}
