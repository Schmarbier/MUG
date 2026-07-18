using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Infrastructure.Persistencia;

namespace PersonalFinance.Infrastructure.Tests.Persistencia;

public sealed class ConvertersTests : IDisposable
{
    private readonly string _rutaBd;
    private readonly PersonalFinanceDbContext _contexto;

    public ConvertersTests()
    {
        _rutaBd = Path.Combine(Path.GetTempPath(), $"personalfinance-converters-{Guid.NewGuid():N}.db");
        var opciones = new DbContextOptionsBuilder<PersonalFinanceDbContext>()
            .UseSqlite($"Data Source={_rutaBd}")
            .Options;
        _contexto = new PersonalFinanceDbContext(opciones);
        _contexto.Database.EnsureCreated();
    }

    [Theory]
    [InlineData(1465.05)]
    [InlineData(0.01)]
    [InlineData(999999.99)]
    [InlineData(100.00)]
    public void Monto_hace_round_trip_decimal_a_INTEGER_sin_perdida(decimal monto)
    {
        var categoria = new Categoria { Titulo = $"Cat-{Guid.NewGuid():N}", Descripcion = "d", Activa = true };
        var moneda = new Moneda { Codigo = $"C{Guid.NewGuid():N}"[..4], EsBase = false, Activa = true, TipoDeCambio = 500m };
        var mensaje = new Mensaje
        {
            IdentificadorCanal = Random.Shared.NextInt64(),
            Texto = "t",
            FechaRecepcionUtc = DateTimeOffset.UtcNow,
            Procesado = true,
            IntentosClasificacion = 0,
            TieneError = false
        };
        _contexto.AddRange(categoria, moneda, mensaje);
        _contexto.SaveChanges();

        var movimiento = new Movimiento
        {
            MensajeId = mensaje.Id,
            CategoriaId = categoria.Id,
            MonedaId = moneda.Id,
            Monto = monto,
            Tipo = TipoMovimiento.Egreso,
            Fecha = DateOnly.FromDateTime(DateTime.Today),
            TipoDeCambioHistorico = 500m
        };
        _contexto.Movimientos.Add(movimiento);
        _contexto.SaveChanges();
        _contexto.ChangeTracker.Clear();

        var recargado = _contexto.Movimientos.Single(m => m.Id == movimiento.Id);
        Assert.Equal(monto, recargado.Monto);
        Assert.Equal(500m, recargado.TipoDeCambioHistorico);
    }

    [Theory]
    [InlineData(1234.56)]
    [InlineData(0.01)]
    public void TipoDeCambio_hace_round_trip_decimal_a_INTEGER_sin_perdida(decimal tipoDeCambio)
    {
        var moneda = new Moneda { Codigo = $"C{Guid.NewGuid():N}"[..4], EsBase = false, Activa = true, TipoDeCambio = tipoDeCambio };
        _contexto.Monedas.Add(moneda);
        _contexto.SaveChanges();
        _contexto.ChangeTracker.Clear();

        var recargada = _contexto.Monedas.Single(m => m.Id == moneda.Id);
        Assert.Equal(tipoDeCambio, recargada.TipoDeCambio);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_rutaBd)) File.Delete(_rutaBd);
    }
}
