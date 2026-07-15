using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
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
        services.AddScoped<IRepositorioCategorias, RepositorioCategorias>();
        services.AddScoped<IRepositorioMonedas, RepositorioMonedas>();
        services.AddScoped<IRepositorioMovimientos, RepositorioMovimientos>();
        services.AddScoped<IConsultaResumen, ConsultaResumen>();
        return services;
    }

    // Registra el pipeline de clasificación: agente Ollama + servicios de dominio.
    public static IServiceCollection AgregarClasificacion(
        this IServiceCollection services, string ollamaUrl, string modelo)
    {
        services.AddSingleton<IOllamaApiClient>(_ => new OllamaApiClient(new Uri(ollamaUrl), modelo));
        services.AddScoped<IAgenteClasificador, AgenteClasificadorOllama>();
        services.AddScoped<ServicioClasificacion>();
        services.AddScoped<ServicioProcesamiento>();
        return services;
    }
}
