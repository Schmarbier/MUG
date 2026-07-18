using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Infrastructure.Persistencia;
using PersonalFinance.Infrastructure.Persistencia.Repositorios;
using PersonalFinance.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<PersonalFinanceDbContext>(opciones =>
    opciones.UseSqlite(ConexionSqlite.ObtenerCadenaConexion()));

builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<IMonedaRepositorio, MonedaRepositorio>();
builder.Services.AddScoped<IMensajeRepositorio, MensajeRepositorio>();
builder.Services.AddScoped<IMovimientoRepositorio, MovimientoRepositorio>();

builder.Services.AddScoped<ResumenMensualServicio>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var contexto = scope.ServiceProvider.GetRequiredService<PersonalFinanceDbContext>();
    contexto.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
