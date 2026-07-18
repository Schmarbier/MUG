using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using PersonalFinance.Bot;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Infrastructure.IA;
using PersonalFinance.Infrastructure.Persistencia;
using PersonalFinance.Infrastructure.Persistencia.Repositorios;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<PersonalFinanceDbContext>(opciones =>
    opciones.UseSqlite(ConexionSqlite.ObtenerCadenaConexion()));

builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<IMonedaRepositorio, MonedaRepositorio>();
builder.Services.AddScoped<IMensajeRepositorio, MensajeRepositorio>();
builder.Services.AddScoped<IMovimientoRepositorio, MovimientoRepositorio>();

builder.Services.AddScoped(sp => new IngestaServicio(
    sp.GetRequiredService<IMensajeRepositorio>(),
    builder.Configuration.GetValue<long>("TelegramChatAutorizado")));

builder.Services.AddScoped<ClasificacionServicio>();

builder.Services.AddSingleton<IOllamaApiClient>(_ =>
    new OllamaApiClient(new Uri("http://localhost:11434"), builder.Configuration["OLLAMA_MODEL"] ?? "llama3.1"));

builder.Services.AddScoped<IClasificadorDeMensajes>(sp => new OllamaClasificadorAdapter(
    sp.GetRequiredService<IOllamaApiClient>(),
    builder.Configuration["OLLAMA_MODEL"] ?? "llama3.1",
    timeout: TimeSpan.FromSeconds(4.5)));

builder.Services.AddSingleton<ITelegramBotClient>(_ =>
    new TelegramBotClient(builder.Configuration["TelegramBotToken"] ?? string.Empty));

builder.Services.AddHostedService<IngestaTelegramBackgroundService>();
builder.Services.AddHostedService<BarridoClasificacionBackgroundService>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var contexto = scope.ServiceProvider.GetRequiredService<PersonalFinanceDbContext>();
    contexto.Database.Migrate();
}

host.Run();
