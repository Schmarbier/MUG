using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Infrastructure.Persistencia.Converters;

namespace PersonalFinance.Infrastructure.Persistencia.Configuraciones;

public class MonedaConfiguracion : IEntityTypeConfiguration<Moneda>
{
    public void Configure(EntityTypeBuilder<Moneda> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Codigo).IsRequired();
        builder.Property(m => m.TipoDeCambio).HasConversion(new TipoDeCambioValueConverter());

        // Único (FR-033).
        builder.HasIndex(m => m.Codigo).IsUnique();
    }
}
