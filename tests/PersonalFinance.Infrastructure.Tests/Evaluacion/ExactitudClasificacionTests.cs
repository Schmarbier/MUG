using OllamaSharp;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Infrastructure.IA;

namespace PersonalFinance.Infrastructure.Tests.Evaluacion;

/// <summary>
/// SC-001: acierto de clasificación ≥ 80% (categoría + tipo, R8) sobre un conjunto etiquetado
/// de al menos 50 mensajes que cubra todas las categorías. Requiere Ollama real corriendo con
/// el modelo llama3.1 — no es parte de la corrida normal de `dotnet test` (ver [Fact(Skip)]).
/// </summary>
public sealed class ExactitudClasificacionTests
{
    private static readonly IReadOnlyList<CategoriaActiva> Categorias =
    [
        new("Hogar", "Alquiler, expensas, limpieza y mantenimiento del hogar"),
        new("Supermercado", "Compras de comida y artículos de almacén"),
        new("Transporte", "Colectivo, subte, nafta, taxi, Uber"),
        new("Salud", "Médicos, farmacia, obra social"),
        new("Ocio", "Salidas, cine, bares, entretenimiento"),
        new("Restaurantes", "Comida afuera, delivery"),
        new("Educacion", "Cursos, libros, universidad"),
        new("Ropa", "Indumentaria y calzado"),
        new("Tecnologia", "Electrónica, software, celulares"),
        new("Mascotas", "Veterinaria y alimento de mascotas"),
        new("Regalos", "Regalos para otras personas"),
        new("Servicios", "Luz, gas, agua, internet, celular"),
        new("Sueldo", "Ingreso por trabajo en relación de dependencia"),
        new("Ahorro", "Ingresos por inversiones o ahorro"),
        new("Otros", "Gastos e ingresos que no encajan en otra categoría"),
    ];

    private static readonly IReadOnlyList<MonedaActiva> Monedas = [new("ARS", true)];

    private static readonly (string Texto, string CategoriaEsperada, TipoMovimiento TipoEsperado)[] Dataset =
    [
        ("15000 alquiler", "Hogar", TipoMovimiento.Egreso),
        ("8000 expensas del depto", "Hogar", TipoMovimiento.Egreso),
        ("3000 productos de limpieza", "Hogar", TipoMovimiento.Egreso),
        ("2500 foquitos y pilas", "Hogar", TipoMovimiento.Egreso),
        ("12000 super de la semana", "Supermercado", TipoMovimiento.Egreso),
        ("4500 verdulería", "Supermercado", TipoMovimiento.Egreso),
        ("3200 carnicería", "Supermercado", TipoMovimiento.Egreso),
        ("18000 changuito grande del chino", "Supermercado", TipoMovimiento.Egreso),
        ("2000 colectivo", "Transporte", TipoMovimiento.Egreso),
        ("15000 nafta", "Transporte", TipoMovimiento.Egreso),
        ("3500 uber al centro", "Transporte", TipoMovimiento.Egreso),
        ("6000 subte carga", "Transporte", TipoMovimiento.Egreso),
        ("8000 farmacia", "Salud", TipoMovimiento.Egreso),
        ("25000 consulta con el dermatólogo", "Salud", TipoMovimiento.Egreso),
        ("12000 cuota obra social", "Salud", TipoMovimiento.Egreso),
        ("4000 ibuprofeno y vendas", "Salud", TipoMovimiento.Egreso),
        ("10000 cine con amigos", "Ocio", TipoMovimiento.Egreso),
        ("15000 entradas para el recital", "Ocio", TipoMovimiento.Egreso),
        ("6000 previa con birra", "Ocio", TipoMovimiento.Egreso),
        ("9000 boliche el sábado", "Ocio", TipoMovimiento.Egreso),
        ("7000 pizza a la noche", "Restaurantes", TipoMovimiento.Egreso),
        ("12000 asado con amigos afuera", "Restaurantes", TipoMovimiento.Egreso),
        ("5000 delivery de sushi", "Restaurantes", TipoMovimiento.Egreso),
        ("3000 café y medialunas", "Restaurantes", TipoMovimiento.Egreso),
        ("20000 curso de inglés", "Educacion", TipoMovimiento.Egreso),
        ("8000 libros para la facu", "Educacion", TipoMovimiento.Egreso),
        ("15000 cuota de la universidad privada", "Educacion", TipoMovimiento.Egreso),
        ("30000 zapatillas nuevas", "Ropa", TipoMovimiento.Egreso),
        ("12000 remeras de invierno", "Ropa", TipoMovimiento.Egreso),
        ("8000 campera de abrigo", "Ropa", TipoMovimiento.Egreso),
        ("45000 auriculares bluetooth", "Tecnologia", TipoMovimiento.Egreso),
        ("120000 notebook nueva", "Tecnologia", TipoMovimiento.Egreso),
        ("6000 suscripción de software", "Tecnologia", TipoMovimiento.Egreso),
        ("15000 veterinario para el gato", "Mascotas", TipoMovimiento.Egreso),
        ("8000 alimento balanceado para el perro", "Mascotas", TipoMovimiento.Egreso),
        ("5000 juguetes para el gato", "Mascotas", TipoMovimiento.Egreso),
        ("10000 regalo de cumpleaños para mi hermana", "Regalos", TipoMovimiento.Egreso),
        ("15000 regalo de casamiento", "Regalos", TipoMovimiento.Egreso),
        ("6000 flores para mi mamá", "Regalos", TipoMovimiento.Egreso),
        ("18000 factura de luz", "Servicios", TipoMovimiento.Egreso),
        ("9000 factura de gas", "Servicios", TipoMovimiento.Egreso),
        ("12000 internet y cable", "Servicios", TipoMovimiento.Egreso),
        ("8000 abono del celular", "Servicios", TipoMovimiento.Egreso),
        ("850000 cobré el sueldo", "Sueldo", TipoMovimiento.Ingreso),
        ("50000 aguinaldo", "Sueldo", TipoMovimiento.Ingreso),
        ("120000 me pagaron un extra por el proyecto", "Sueldo", TipoMovimiento.Ingreso),
        ("30000 intereses del plazo fijo", "Ahorro", TipoMovimiento.Ingreso),
        ("45000 dividendos de las acciones", "Ahorro", TipoMovimiento.Ingreso),
        ("20000 rendimiento del fondo común", "Ahorro", TipoMovimiento.Ingreso),
        ("5000 imprevisto varios", "Otros", TipoMovimiento.Egreso),
        ("8000 gasto que no sé bien en qué se fue", "Otros", TipoMovimiento.Egreso),
        ("10000 me devolvieron una plata que había prestado", "Otros", TipoMovimiento.Ingreso),
    ];

    [Fact(Skip = "Requiere Ollama corriendo con llama3.1. Ejecutado manualmente el 2026-07-18: 46/52 (88.5%), por encima del umbral de SC-001 tras cambiar Format a un esquema JSON estructurado (ver commit).")]
    public async Task Acierto_de_clasificacion_es_al_menos_80_por_ciento()
    {
        var cliente = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.1");
        var adaptador = new OllamaClasificadorAdapter(cliente, "llama3.1", TimeSpan.FromSeconds(10));

        var aciertos = 0;
        var fallos = new List<string>();

        foreach (var (texto, categoriaEsperada, tipoEsperado) in Dataset)
        {
            var resultado = await adaptador.ClasificarAsync(texto, Categorias, Monedas);

            var acierto = resultado is ResultadoClasificacion.Exitosa exitosa
                && exitosa.Clasificacion.TituloCategoria == categoriaEsperada
                && exitosa.Clasificacion.Tipo == tipoEsperado;

            if (acierto)
            {
                aciertos++;
            }
            else
            {
                var obtenido = resultado is ResultadoClasificacion.Exitosa e
                    ? $"{e.Clasificacion.TituloCategoria}/{e.Clasificacion.Tipo}"
                    : $"Falla:{((ResultadoClasificacion.Fallida)resultado).Falla.Motivo}";
                fallos.Add($"\"{texto}\" esperado {categoriaEsperada}/{tipoEsperado}, obtenido {obtenido}");
            }
        }

        var porcentaje = aciertos * 100.0 / Dataset.Length;
        Console.WriteLine($"RESULTADO_SC001: {aciertos}/{Dataset.Length} = {porcentaje:F1}%");

        Assert.True(
            porcentaje >= 80,
            $"Acierto {porcentaje:F1}% ({aciertos}/{Dataset.Length}), se esperaba >= 80%.\n" +
            string.Join("\n", fallos));
    }
}
