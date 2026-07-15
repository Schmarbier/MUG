using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain;
using PersonalFinance.Infrastructure;
using PersonalFinance.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

// Misma persistencia que el Bot (mismo archivo SQLite, ver AGENTS.md).
builder.Services.AgregarPersistencia();
builder.Services.AddScoped<ServicioResumen>();

var app = builder.Build();

// Crea/actualiza la base si la Web corre antes que el Bot (Migrate es idempotente).
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
