using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PersonalFinance.Infrastructure.Persistencia;

namespace PersonalFinance.Infrastructure.Tests;

/// <summary>
/// Base SQLite in-memory con cache compartida. La conexión ancla se mantiene abierta porque una
/// base en memoria vive mientras haya al menos una conexión abierta; con <c>Cache=Shared</c>
/// varias conexiones —incluidas las que simulan otro proceso— ven la misma base.
/// </summary>
internal sealed class BaseDePruebas : IAsyncDisposable
{
    private readonly SqliteConnection _ancla;

    private BaseDePruebas(string cadenaConexion, SqliteConnection ancla)
    {
        CadenaConexion = cadenaConexion;
        _ancla = ancla;
    }

    public string CadenaConexion { get; }

    public static async Task<BaseDePruebas> CrearAsync()
    {
        var cadena = $"Data Source=pruebas-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        var ancla = new SqliteConnection(cadena);
        await ancla.OpenAsync(CancellationToken.None);

        var baseDePruebas = new BaseDePruebas(cadena, ancla);

        await using var contexto = baseDePruebas.NuevoContexto();
        await contexto.Database.EnsureCreatedAsync(CancellationToken.None);

        return baseDePruebas;
    }

    public PersonalFinanceDbContext NuevoContexto(params IInterceptor[] interceptores)
    {
        var opciones = new DbContextOptionsBuilder<PersonalFinanceDbContext>()
            .UseSqlite(CadenaConexion)
            .AddInterceptors(interceptores)
            .Options;

        return new PersonalFinanceDbContext(opciones);
    }

    public async ValueTask DisposeAsync() => await _ancla.DisposeAsync();
}
