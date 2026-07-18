using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Infrastructure.Persistencia;
using PersonalFinance.Infrastructure.Persistencia.Repositorios;

namespace PersonalFinance.Infrastructure.Tests.Persistencia;

public sealed class IngestaServicioTests : IDisposable
{
    private const long ChatAutorizado = 100L;

    private readonly string _rutaBd;
    private readonly PersonalFinanceDbContext _contexto;
    private readonly IngestaServicio _servicio;

    public IngestaServicioTests()
    {
        _rutaBd = Path.Combine(Path.GetTempPath(), $"personalfinance-ingesta-{Guid.NewGuid():N}.db");
        var opciones = new DbContextOptionsBuilder<PersonalFinanceDbContext>()
            .UseSqlite($"Data Source={_rutaBd}")
            .Options;
        _contexto = new PersonalFinanceDbContext(opciones);
        _contexto.Database.Migrate();

        _servicio = new IngestaServicio(new MensajeRepositorio(_contexto), ChatAutorizado);
    }

    [Fact]
    public async Task Reingerir_el_mismo_identificador_de_canal_no_duplica_el_mensaje()
    {
        await _servicio.IngerirAsync(ChatAutorizado, 555L, "2000 en super", DateTimeOffset.UtcNow);
        await _servicio.IngerirAsync(ChatAutorizado, 555L, "2000 en super (reenviado)", DateTimeOffset.UtcNow);

        Assert.Equal(1, await _contexto.Mensajes.CountAsync());
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_rutaBd)) File.Delete(_rutaBd);
    }
}
