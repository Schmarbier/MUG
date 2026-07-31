using PersonalFinance.Domain.Entidades;
using Xunit;

namespace PersonalFinance.Domain.Tests;

public class CategoriaTests
{
    // Sustenta AC-04 desde el dominio: las categorías del seed nacen con estado activa.
    [Fact]
    public void Constructor_DatosValidos_QuedaActiva()
    {
        Categoria categoria = new(titulo: "Sueldo", descripcion: "Ingresos por trabajo en relación de dependencia.");

        Assert.True(categoria.Activa);
    }

    // Sustenta AC-04: la categoría conserva el título y la descripción con los que se creó.
    [Fact]
    public void Constructor_DatosValidos_ConservaTituloYDescripcion()
    {
        Categoria categoria = new(titulo: "Hogar", descripcion: "Gastos de la casa: comida, limpieza, mantenimiento.");

        Assert.Equal(
            ("Hogar", "Gastos de la casa: comida, limpieza, mantenimiento."),
            (categoria.Titulo, categoria.Descripcion));
    }

    // Sad path de AC-04: una categoría sin título no es identificable ni puede tener título único.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_TituloVacio_LanzaArgumentException(string titulo)
    {
        ArgumentException excepcion = Assert.Throws<ArgumentException>(
            () => new Categoria(titulo: titulo, descripcion: "Una descripción válida."));

        Assert.Equal("titulo", excepcion.ParamName);
    }

    // Sad path de AC-04: el título tiene un máximo de 60 caracteres.
    [Fact]
    public void Constructor_TituloMayorA60_LanzaArgumentOutOfRangeException()
    {
        string titulo = new('t', Categoria.TituloMaximo + 1);

        ArgumentOutOfRangeException excepcion = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Categoria(titulo: titulo, descripcion: "Una descripción válida."));

        Assert.Equal("titulo", excepcion.ParamName);
    }

    // Sad path de AC-04: la descripción alimenta el prompt del clasificador (FR-08); vacía no sirve.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_DescripcionVacia_LanzaArgumentException(string descripcion)
    {
        ArgumentException excepcion = Assert.Throws<ArgumentException>(
            () => new Categoria(titulo: "Ocio", descripcion: descripcion));

        Assert.Equal("descripcion", excepcion.ParamName);
    }

    // Sad path de AC-04: la descripción tiene un máximo de 200 caracteres.
    [Fact]
    public void Constructor_DescripcionMayorA200_LanzaArgumentOutOfRangeException()
    {
        string descripcion = new('d', Categoria.DescripcionMaximo + 1);

        ArgumentOutOfRangeException excepcion = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Categoria(titulo: "Ocio", descripcion: descripcion));

        Assert.Equal("descripcion", excepcion.ParamName);
    }
}
