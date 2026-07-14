namespace PersonalFinance.Domain;

// Paginación pura de las filas del resumen. Cada bloque se pagina de forma independiente,
// de a 4 filas (categoría+moneda) por página (RF-13). numeroPagina es 1-based.
public static class Paginador
{
    public const int TamanioResumen = 4;

    public static int TotalPaginas(int totalItems, int tamanio = TamanioResumen)
        => totalItems <= 0 ? 1 : (int)Math.Ceiling(totalItems / (double)tamanio);

    public static IReadOnlyList<T> Pagina<T>(IReadOnlyList<T> items, int numeroPagina, int tamanio = TamanioResumen)
        => items.Skip((numeroPagina - 1) * tamanio).Take(tamanio).ToList();
}
