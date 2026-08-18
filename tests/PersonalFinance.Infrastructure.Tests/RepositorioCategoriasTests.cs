using PersonalFinance.Domain.Entidades;
using PersonalFinance.Infrastructure.Persistencia;
using Xunit;

namespace PersonalFinance.Infrastructure.Tests;

public class RepositorioCategoriasTests
{
    // Valida FR-08: sólo las categorías activas participan de la clasificación. El filtro vive
    // acá, en el adaptador del puerto, y es lo único que impide que una categoría desactivada
    // llegue al prompt del clasificador y el modelo pueda elegirla.
    [Fact]
    public async Task ObtenerActivasAsync_ConActivasYDesactivadas_DevuelveSoloLasActivas()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        await using var contexto = baseDatos.NuevoContexto();
        await GuardarAsync(contexto, ("Hogar", true), ("Ocio", false), ("Sueldo", true));

        await using var verificacion = baseDatos.NuevoContexto();
        var activas = await new RepositorioCategoriasEfCore(verificacion)
            .ObtenerActivasAsync(CancellationToken.None);

        Assert.Equal(["Hogar", "Sueldo"], activas.Select(c => c.Titulo));
    }

    // Sad path de FR-08 y precondición del caso de uso del Bloque 5: con todas las categorías
    // desactivadas no queda ninguna para clasificar, y el repositorio devuelve la lista vacía en
    // vez de caer de vuelta en el seed completo.
    [Fact]
    public async Task ObtenerActivasAsync_TodasDesactivadas_DevuelveListaVacia()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        await using var contexto = baseDatos.NuevoContexto();
        await GuardarAsync(contexto, ("Hogar", false), ("Ocio", false));

        await using var verificacion = baseDatos.NuevoContexto();
        var activas = await new RepositorioCategoriasEfCore(verificacion)
            .ObtenerActivasAsync(CancellationToken.None);

        Assert.Empty(activas);
    }

    /// <summary>
    /// Arma el estado de la base. La desactivación se hace por el tracker de EF y no por un
    /// método de dominio a propósito: desactivar categorías no es parte de este ticket, así que
    /// <see cref="Categoria"/> no expone cómo hacerlo y el estado se escribe desde la
    /// infraestructura, que es de donde vendría en la base real.
    /// </summary>
    private static async Task GuardarAsync(
        PersonalFinanceDbContext contexto,
        params (string Titulo, bool Activa)[] categorias)
    {
        foreach (var (titulo, activa) in categorias)
        {
            var categoria = new Categoria(titulo, $"Descripcion de {titulo}.");
            contexto.Categorias.Add(categoria);
            contexto.Entry(categoria).Property(c => c.Activa).CurrentValue = activa;
        }

        await contexto.SaveChangesAsync(CancellationToken.None);
    }
}
