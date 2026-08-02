using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Infrastructure.Persistencia;

/// <summary>
/// Crea las 5 categorías del seed (FR-05). Es idempotente por diseño: correrlo N veces deja
/// exactamente 5 categorías.
/// </summary>
public sealed class SeedCategorias
{
    /// <summary>
    /// Las descripciones no son decorativas: el Bloque 4 las manda en el system prompt para que
    /// el clasificador sepa qué cae en cada categoría.
    /// </summary>
    private static readonly (string Titulo, string Descripcion)[] Semilla =
    [
        ("Hogar", "Gastos de la casa: comida, supermercado, alquiler, expensas y mantenimiento."),
        ("Ocio", "Salidas, restaurantes, entretenimiento, viajes, suscripciones y hobbies."),
        ("Servicios", "Luz, gas, agua, internet, telefonía, seguros e impuestos."),
        ("Sueldo", "Ingresos por trabajo: sueldo, aguinaldo, honorarios y pagos de clientes."),
        ("Otros", "Categoría de descarte: lo que no encaja en ninguna de las anteriores."),
    ];

    private readonly PersonalFinanceDbContext _contexto;

    public SeedCategorias(PersonalFinanceDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    public async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        // Es la primera versión del esquema y no hay migraciones (FEAT-001f las introducirá al
        // agregar el campo moneda). Crear el esquema acá deja al composition root sin ninguna
        // llamada a EF Core.
        await _contexto.Database.EnsureCreatedAsync(cancellationToken);

        var existentes = await _contexto.Categorias
            .Select(c => c.Titulo)
            .ToListAsync(cancellationToken);

        foreach (var (titulo, descripcion) in Semilla)
        {
            if (existentes.Contains(titulo))
            {
                continue;
            }

            var categoria = new Categoria(titulo, descripcion);
            _contexto.Categorias.Add(categoria);

            try
            {
                // Una confirmación por categoría: si otra corrida ganó la carrera por este
                // título, sólo se pierde esta inserción y las demás siguen.
                await _contexto.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Otro proceso insertó el mismo título entre la lectura y la escritura. La
                // categoría ya está donde tiene que estar: se descarta el intento y se sigue.
                _contexto.Entry(categoria).State = EntityState.Detached;
            }
        }
    }
}
