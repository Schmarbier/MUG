using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PersonalFinance.Infrastructure.Persistencia;

/// <summary>Usado solo por las herramientas de diseño de EF Core (dotnet ef migrations).</summary>
public class PersonalFinanceDbContextFactory : IDesignTimeDbContextFactory<PersonalFinanceDbContext>
{
    public PersonalFinanceDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<PersonalFinanceDbContext>()
            .UseSqlite(ConexionSqlite.ObtenerCadenaConexion())
            .Options;

        return new PersonalFinanceDbContext(opciones);
    }
}
