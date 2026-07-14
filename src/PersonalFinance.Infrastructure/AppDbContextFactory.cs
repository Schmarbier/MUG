using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PersonalFinance.Infrastructure;

// Permite a `dotnet ef` instanciar el contexto en tiempo de diseño (para migraciones),
// aunque este proyecto sea una class library sin host.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=personalfinance.db")
            .Options;
        return new AppDbContext(opciones);
    }
}
