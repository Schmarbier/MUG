using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure;

public static class ServiceCollectionExtensions
{
    // Bot y Web DEBEN apuntar al mismo archivo SQLite. Ruta relativa al directorio de
    // trabajo: ambos procesos se corren desde la raíz del repo (ver AGENTS.md).
    public const string CadenaConexionPorDefecto = "Data Source=personalfinance.db";

    public static IServiceCollection AgregarPersistencia(
        this IServiceCollection services, string? cadenaConexion = null)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(cadenaConexion ?? CadenaConexionPorDefecto));
        services.AddScoped<IRepositorioMensajes, RepositorioMensajes>();
        return services;
    }
}
