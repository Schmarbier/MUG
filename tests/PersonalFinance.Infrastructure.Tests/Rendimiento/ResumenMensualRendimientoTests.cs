using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Servicios;
using PersonalFinance.Infrastructure.Persistencia;
using PersonalFinance.Infrastructure.Persistencia.Repositorios;

namespace PersonalFinance.Infrastructure.Tests.Rendimiento;

/// <summary>
/// SC-003: resumen mensual &lt; 1 s p95 sobre el volumen de referencia de R7
/// (24 meses × 300 movimientos/mes = 7.200 movimientos, 20 categorías, 3 monedas).
/// </summary>
public sealed class ResumenMensualRendimientoTests : IDisposable
{
    private readonly string _rutaBd;
    private readonly PersonalFinanceDbContext _contexto;

    public ResumenMensualRendimientoTests()
    {
        _rutaBd = Path.Combine(Path.GetTempPath(), $"personalfinance-rendimiento-{Guid.NewGuid():N}.db");
        var opciones = new DbContextOptionsBuilder<PersonalFinanceDbContext>()
            .UseSqlite($"Data Source={_rutaBd}")
            .Options;
        _contexto = new PersonalFinanceDbContext(opciones);
        _contexto.Database.Migrate();
    }

    [Fact]
    public async Task Resumen_mensual_resuelve_en_menos_de_1s_p95_sobre_7200_movimientos()
    {
        SembrarVolumenDeReferencia();

        var movimientoRepositorio = new MovimientoRepositorio(_contexto);
        var categoriaRepositorio = new CategoriaRepositorio(_contexto);
        var monedaRepositorio = new MonedaRepositorio(_contexto);
        var servicio = new ResumenMensualServicio(movimientoRepositorio, categoriaRepositorio, monedaRepositorio);

        // Warm-up: la primera consulta paga el costo de JIT/planificación de SQLite.
        await servicio.ObtenerResumenAsync(2026, 1, 1, 1);

        const int iteraciones = 20;
        var duraciones = new List<double>(iteraciones);
        for (var i = 0; i < iteraciones; i++)
        {
            var cronometro = Stopwatch.StartNew();
            await servicio.ObtenerResumenAsync(2026, (i % 12) + 1, 1, 1);
            cronometro.Stop();
            duraciones.Add(cronometro.Elapsed.TotalMilliseconds);
        }

        duraciones.Sort();
        var indiceP95 = (int)Math.Ceiling(0.95 * duraciones.Count) - 1;
        var p95 = duraciones[indiceP95];

        Assert.True(p95 < 1000, $"p95 fue {p95:F1} ms, se esperaba < 1000 ms. Duraciones: {string.Join(", ", duraciones.Select(d => d.ToString("F1")))}");
    }

    private void SembrarVolumenDeReferencia()
    {
        const int meses = 24;
        const int movimientosPorMes = 300;
        const int cantidadCategorias = 20;

        var categorias = Enumerable.Range(1, cantidadCategorias)
            .Select(i => new Categoria { Titulo = $"Categoria{i}", Descripcion = "d", Activa = true })
            .ToList();
        _contexto.Categorias.AddRange(categorias);

        var usd = new Moneda { Codigo = "USD", EsBase = false, Activa = true, TipoDeCambio = 1500m };
        var eur = new Moneda { Codigo = "EUR", EsBase = false, Activa = true, TipoDeCambio = 1600m };
        _contexto.Monedas.AddRange(usd, eur);
        _contexto.SaveChanges();

        var ars = _contexto.Monedas.Single(m => m.Codigo == "ARS");
        var monedas = new[] { ars, usd, eur };

        var inicio = new DateOnly(2024, 8, 1);
        var random = new Random(Seed: 42);
        var movimientos = new List<Movimiento>(meses * movimientosPorMes);

        for (var mes = 0; mes < meses; mes++)
        {
            var fechaBase = inicio.AddMonths(mes);
            for (var i = 0; i < movimientosPorMes; i++)
            {
                var categoria = categorias[random.Next(cantidadCategorias)];
                var moneda = monedas[random.Next(monedas.Length)];
                movimientos.Add(new Movimiento
                {
                    CategoriaId = categoria.Id,
                    MonedaId = moneda.Id,
                    Monto = Math.Round((decimal)(random.NextDouble() * 5000 + 1), 2),
                    Tipo = random.Next(2) == 0 ? TipoMovimiento.Ingreso : TipoMovimiento.Egreso,
                    Fecha = fechaBase.AddDays(random.Next(1, 28)),
                    TipoDeCambioHistorico = moneda.EsBase ? null : moneda.TipoDeCambio,
                    MensajeId = 0
                });
            }
        }

        // MensajeId=0 no referencia un Mensaje real; no hay FK declarada en la configuración
        // (data-model.md no la exige), así que sembrar el volumen no requiere 7.200 mensajes.
        _contexto.Movimientos.AddRange(movimientos);
        _contexto.SaveChanges();
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_rutaBd)) File.Delete(_rutaBd);
    }
}
