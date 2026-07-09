using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using Telegram.Bot;
using PersonalFinance.Bot.Data;
using PersonalFinance.Bot.Services;
using PersonalFinance.Bot.Telegram;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var token = config["TelegramBotToken"];
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine(
        "Falta TelegramBotToken. Configuralo con:\n" +
        "  dotnet user-secrets set \"TelegramBotToken\" \"<token>\"   (desde src/PersonalFinance.Bot)\n" +
        "o exportá la variable de entorno TelegramBotToken.");
    return 1;
}

var ollamaModel = config["OLLAMA_MODEL"] ?? "llama3.1";
var ollamaUri = new Uri(config["OLLAMA_HOST"] ?? "http://localhost:11434");

// Base de datos: crear/actualizar el esquema al arrancar.
PersonalFinanceContext CrearDb() => new(
    new DbContextOptionsBuilder<PersonalFinanceContext>()
        .UseSqlite($"Data Source={PersonalFinanceContext.DefaultDbFileName}")
        .Options);

await using (var db = CrearDb())
    await db.Database.MigrateAsync();

// Instrucciones del agente clasificador (se copian junto al binario).
var rutaInstrucciones = Path.Combine(AppContext.BaseDirectory, "Prompts", "clasificador.md");
var instrucciones = await File.ReadAllTextAsync(rutaInstrucciones);

var ollama = new OllamaApiClient(ollamaUri, ollamaModel);
var clasificador = new OllamaClasificador(ollama, instrucciones);
var parser = new MensajeParser();

var bot = new TelegramBotClient(token);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

new TelegramListener(bot, CrearDb, parser, clasificador).Start(cts.Token);

var me = await bot.GetMe(cts.Token);
Console.WriteLine($"Bot @{me.Username} escuchando Telegram (modelo Ollama: {ollamaModel}). Ctrl+C para salir.");

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    // Ctrl+C: salida ordenada.
}

return 0;
