using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Infrastructure.Persistencia.Converters;

namespace PersonalFinance.Infrastructure.Persistencia.Configuraciones;

public class MovimientoConfiguracion : IEntityTypeConfiguration<Movimiento>
{
    public void Configure(EntityTypeBuilder<Movimiento> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Monto).HasConversion(new MontoValueConverter());
        builder.Property(m => m.TipoDeCambioHistorico).HasConversion(new TipoDeCambioValueConverter());

        // Consulta del resumen por mes.
        builder.HasIndex(m => m.Fecha);

        // Propagación del tipo de cambio histórico a movimientos de igual moneda y fecha (FR-023).
        builder.HasIndex(m => new { m.MonedaId, m.Fecha });
    }
}
