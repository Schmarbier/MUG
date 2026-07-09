using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Bot.Data;

namespace PersonalFinance.Tests;

/// <summary>
/// Crea un <see cref="PersonalFinanceContext"/> sobre una base SQLite en memoria, con la
/// misma migración/seed que la base real. La conexión se mantiene abierta mientras viva
/// el contexto (requisito de SQLite in-memory).
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public PersonalFinanceContext Context { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PersonalFinanceContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new PersonalFinanceContext(options);
        Context.Database.Migrate();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
