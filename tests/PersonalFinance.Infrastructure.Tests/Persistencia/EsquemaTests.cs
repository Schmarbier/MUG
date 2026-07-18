using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Infrastructure.Persistencia;

namespace PersonalFinance.Infrastructure.Tests.Persistencia;

public sealed class EsquemaTests : IDisposable
{
    private readonly string _rutaBd;
    private readonly PersonalFinanceDbContext _contexto;

    public EsquemaTests()
    {
        _rutaBd = Path.Combine(Path.GetTempPath(), $"personalfinance-esquema-{Guid.NewGuid():N}.db");
        var opciones = new DbContextOptionsBuilder<PersonalFinanceDbContext>()
            .UseSqlite($"Data Source={_rutaBd}")
            .Options;
        _contexto = new PersonalFinanceDbContext(opciones);
        _contexto.Database.EnsureCreated();
    }

    [Fact]
    public void Categoria_con_titulo_duplicado_lanza_excepcion()
    {
        _contexto.Categorias.Add(new Categoria { Titulo = "Hogar", Descripcion = "Gastos del hogar", Activa = true });
        _contexto.SaveChanges();

        _contexto.Categorias.Add(new Categoria { Titulo = "Hogar", Descripcion = "Otra descripción", Activa = true });

        Assert.Throws<DbUpdateException>(() => _contexto.SaveChanges());
    }

    [Fact]
    public void Moneda_con_codigo_duplicado_lanza_excepcion()
    {
        _contexto.Monedas.Add(new Moneda { Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1000m });
        _contexto.SaveChanges();

        _contexto.Monedas.Add(new Moneda { Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1050m });

        Assert.Throws<DbUpdateException>(() => _contexto.SaveChanges());
    }

    [Fact]
    public void Mensaje_con_identificador_de_canal_duplicado_lanza_excepcion()
    {
        _contexto.Mensajes.Add(new Mensaje
        {
            IdentificadorCanal = 123L,
            Texto = "2000 en super",
            FechaRecepcionUtc = DateTimeOffset.UtcNow,
            Procesado = false,
            IntentosClasificacion = 0,
            TieneError = false
        });
        _contexto.SaveChanges();

        _contexto.Mensajes.Add(new Mensaje
        {
            IdentificadorCanal = 123L,
            Texto = "otro texto",
            FechaRecepcionUtc = DateTimeOffset.UtcNow,
            Procesado = false,
            IntentosClasificacion = 0,
            TieneError = false
        });

        Assert.Throws<DbUpdateException>(() => _contexto.SaveChanges());
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_rutaBd)) File.Delete(_rutaBd);
    }
}
