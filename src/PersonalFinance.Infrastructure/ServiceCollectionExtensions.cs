using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using PersonalFinance.Domain;

namespace PersonalFinance.Infrastructure;

public static class ServiceCollectionExtensions
{
    // Bot y Web DEBEN apuntar al mismo archivo SQLite. Ruta absoluta y estable en LocalAppData:
    // NO relativa, porque `dotnet run --project X` usa el directorio del proyecto como working
    // directory y cada proceso terminaría con su propio archivo.
    public static string RutaBaseDatosPorDefecto()
    {
        var carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PersonalFinance");
        Directory.CreateDirectory(carpeta);
        return Path.Combine(carpeta, "personalfinance.db");
    }

    public static IServiceCollection AgregarPersistencia(
        this IServiceCollection services, string? cadenaConexion = null)
    {
        cadenaConexion ??= $"Data Source={RutaBaseDatosPorDefecto()}";
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(cadenaConexion));
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
