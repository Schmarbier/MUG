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
        ("Hogar",
            "Gastos de la casa: comida, supermercado, verdulería, carnicería, alquiler, " +
            "expensas, limpieza y arreglos del hogar."),
        ("Ocio",
            "Tiempo libre y entretenimiento: salidas, bares, restaurantes, cine, recitales, " +
            "viajes, streaming y libros. No incluye regalos, ropa ni cuidado personal."),
        ("Servicios",
            "Cuentas que se pagan periódicamente: luz, gas, agua, internet, telefonía, " +
            "seguros, prepaga e impuestos."),
        ("Sueldo",
            "Plata que entra por trabajo: sueldo, aguinaldo, adelantos, horas extras, " +
            "honorarios y pagos de clientes."),
        ("Otros",
            "Lo que no encaja en las anteriores: regalos, ropa, cuidado personal, préstamos, " +
            "donaciones, ventas de cosas usadas y reintegros."),
    ];

    /// <summary>
    /// Las categorías del seed como entidades. Existe para que el prompt del clasificador se
    /// mida en los tests con las mismas descripciones que corren en producción: duplicarlas en
    /// el test hacía que la accuracy medida fuera la de otro prompt.
    /// </summary>
    public static IReadOnlyList<Categoria> Definiciones() =>
        [.. Semilla.Select(s => new Categoria(s.Titulo, s.Descripcion))];

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
