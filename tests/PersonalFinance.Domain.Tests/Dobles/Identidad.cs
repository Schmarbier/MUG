using PersonalFinance.Domain.Entidades;

namespace PersonalFinance.Domain.Tests.Dobles;

/// <summary>
/// Asigna el Id de una entidad recién construida.
/// <para>
/// En producción el Id lo pone la base al persistir, así que las entidades no exponen setter
/// —y está bien que no lo hagan: un Id que puede escribirse desde el negocio es un Id que se
/// puede pisar—. Pero un test de dominio no toca la base, y sin Ids distintos no se puede
/// afirmar que un movimiento quedó en la categoría correcta y no en otra. Se resuelve acá, en
/// el borde de los dobles, en vez de abrirle una puerta al dominio.
/// </para>
/// </summary>
internal static class Identidad
{
    public static Categoria ConId(this Categoria categoria, int id) => Asignar(categoria, nameof(Categoria.Id), id);

    public static Mensaje ConId(this Mensaje mensaje, long id) => Asignar(mensaje, nameof(Mensaje.Id), id);

    private static T Asignar<T>(T entidad, string propiedad, object valor)
        where T : notnull
    {
        var setter = typeof(T).GetProperty(propiedad)?.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException($"{typeof(T).Name} no tiene la propiedad {propiedad}.");

        setter.Invoke(entidad, [valor]);

        return entidad;
    }
}
