using Microsoft.EntityFrameworkCore;
using PersonalFinance.Bot;
using PersonalFinance.Domain;
using PersonalFinance.Infrastructure;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// El token no se commitea: viene de user-secrets o variable de entorno (ver AGENTS.md).
var token = builder.Configuration["TelegramBotToken"];
if (string.IsNullOrWhiteSpace(token))
    throw new InvalidOperationException(
        "Falta 'TelegramBotToken'. Cargalo con `dotnet user-secrets set` o variable de entorno (ver AGENTS.md).");

// Chat autorizado del dueño (RF-02): solo se ingieren mensajes de este chat.
var chatAutorizado = builder.Configuration.GetValue<long>("TelegramChatAutorizado");

// Ollama corre aparte; modelo configurable por OLLAMA_MODEL (ver AGENTS.md).
var ollamaUrl = builder.Configuration["OllamaUrl"] ?? "http://localhost:11434";
var modelo = builder.Configuration["OLLAMA_MODEL"] ?? "llama3.1";

builder.Services.AgregarPersistencia();
builder.Services.AgregarClasificacion(ollamaUrl, modelo);
builder.Services.AddScoped(sp => new ServicioIngesta(
    sp.GetRequiredService<IRepositorioMensajes>(), chatAutorizado));
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
builder.Services.AddHostedService<TelegramIngestaWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    if (chatAutorizado == 0)
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>()
            .LogWarning("[ingesta] TelegramChatAutorizado = 0: no se ingerirá ningún mensaje hasta configurarlo.");
}

host.Run();
