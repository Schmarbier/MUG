using PersonalFinance.Domain.Entidades;
using Xunit;

namespace PersonalFinance.Domain.Tests;

public class MovimientoTests
{
    private static readonly DateTime FechaCreacion = new(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);

    // Sustenta AC-06 y AC-07: el movimiento que sale de clasificar un mensaje queda con su monto,
    // su tipo, su categoría y el mensaje del que salió.
    [Fact]
    public void Crear_DatosValidos_CreaMovimiento()
    {
        Movimiento movimiento = Movimiento.Crear(
            mensajeId: 42,
            categoriaId: 4,
            monto: 10000m,
            tipo: TipoMovimiento.Ingreso,
            fechaCreacion: FechaCreacion);

        Assert.Equal(
            (42L, 4, 10000m, TipoMovimiento.Ingreso, FechaCreacion),
            (movimiento.MensajeId, movimiento.CategoriaId, movimiento.Monto, movimiento.Tipo, movimiento.FechaCreacion));
    }

    // Sad path de AC-06/AC-07: un movimiento sin monto positivo no es un movimiento.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10000.50)]
    public void Crear_MontoCeroONegativo_LanzaArgumentOutOfRangeException(decimal monto)
    {
        ArgumentOutOfRangeException excepcion = Assert.Throws<ArgumentOutOfRangeException>(
            () => Movimiento.Crear(
                mensajeId: 42,
                categoriaId: 1,
                monto: monto,
                tipo: TipoMovimiento.Egreso,
                fechaCreacion: FechaCreacion));

        Assert.Equal("monto", excepcion.ParamName);
    }

    // Sad path de AC-11: el tipo sólo puede ser ingreso o egreso; un int fuera del enum se rechaza.
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public void Crear_TipoFueraDelEnum_LanzaArgumentOutOfRangeException(int tipo)
    {
        ArgumentOutOfRangeException excepcion = Assert.Throws<ArgumentOutOfRangeException>(
            () => Movimiento.Crear(
                mensajeId: 42,
                categoriaId: 1,
                monto: 10000m,
                tipo: (TipoMovimiento)tipo,
                fechaCreacion: FechaCreacion));

        Assert.Equal("tipo", excepcion.ParamName);
    }
}
