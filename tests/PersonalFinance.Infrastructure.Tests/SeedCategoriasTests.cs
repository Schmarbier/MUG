using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PersonalFinance.Domain.Entidades;
using PersonalFinance.Infrastructure.Persistencia;
using Xunit;

namespace PersonalFinance.Infrastructure.Tests;

public class SeedCategoriasTests
{
    private static readonly string[] SeedEsperado = ["Hogar", "Ocio", "Servicios", "Sueldo", "Otros"];

    // Valida AC-04: al inicializarse el sistema existen las 5 categorías del seed, activas.
    [Fact]
    public async Task EjecutarAsync_BaseVacia_CreaLasCincoCategoriasActivas()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        await using var contexto = baseDatos.NuevoContexto();

        await new SeedCategorias(contexto).EjecutarAsync(CancellationToken.None);

        await using var verificacion = baseDatos.NuevoContexto();
        var categorias = await verificacion.Categorias.ToListAsync(CancellationToken.None);
        Assert.Equal(SeedEsperado.Order(), categorias.Select(c => c.Titulo).Order());
        Assert.All(categorias, c => Assert.True(c.Activa));
    }

    // Valida AC-05: correr el seed de nuevo no duplica categorías.
    [Fact]
    public async Task EjecutarAsync_SeedYaExistente_DejaLaCantidadEnCinco()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        await using var primera = baseDatos.NuevoContexto();
        await new SeedCategorias(primera).EjecutarAsync(CancellationToken.None);

        await using var segunda = baseDatos.NuevoContexto();
        await new SeedCategorias(segunda).EjecutarAsync(CancellationToken.None);

        await using var verificacion = baseDatos.NuevoContexto();
        Assert.Equal(5, await verificacion.Categorias.CountAsync(CancellationToken.None));
    }

    // Sad path de AC-05: con el seed a medias inserta sólo las que faltan y no pisa las que ya
    // están (la descripción preexistente de Hogar sigue intacta).
    [Fact]
    public async Task EjecutarAsync_SeedParcial_InsertaSoloLasFaltantes()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        await using var preexistente = baseDatos.NuevoContexto();
        preexistente.Categorias.Add(new Categoria("Hogar", "descripcion previa"));
        preexistente.Categorias.Add(new Categoria("Otros", "descripcion previa"));
        await preexistente.SaveChangesAsync(CancellationToken.None);

        await using var contexto = baseDatos.NuevoContexto();
        await new SeedCategorias(contexto).EjecutarAsync(CancellationToken.None);

        await using var verificacion = baseDatos.NuevoContexto();
        var categorias = await verificacion.Categorias.ToListAsync(CancellationToken.None);
        Assert.Equal(SeedEsperado.Order(), categorias.Select(c => c.Titulo).Order());
        Assert.Equal("descripcion previa", categorias.Single(c => c.Titulo == "Hogar").Descripcion);
    }

    // Sad path del error documentado: otro proceso inserta el mismo título entre la lectura y la
    // escritura. El seed absorbe la DbUpdateException y termina la corrida igual.
    [Fact]
    public async Task EjecutarAsync_TituloDuplicadoPorCarrera_NoPropagaDbUpdateException()
    {
        await using var baseDatos = await BaseDePruebas.CrearAsync();
        var carrera = new InsertaCategoriaAlGuardar(baseDatos.CadenaConexion, "Hogar");
        await using var contexto = baseDatos.NuevoContexto(carrera);

        await new SeedCategorias(contexto).EjecutarAsync(CancellationToken.None);

        Assert.True(carrera.Disparo);
        await using var verificacion = baseDatos.NuevoContexto();
        var categorias = await verificacion.Categorias.ToListAsync(CancellationToken.None);
        Assert.Equal(SeedEsperado.Order(), categorias.Select(c => c.Titulo).Order());
    }

    /// <summary>
    /// Simula el otro proceso: en la primera confirmación del seed inserta la categoría en
    /// disputa desde otra conexión, de modo que el INSERT que viene a continuación viole el
    /// índice único de <c>Titulo</c>.
    /// </summary>
    private sealed class InsertaCategoriaAlGuardar : SaveChangesInterceptor
    {
        private readonly string _cadenaConexion;
        private readonly string _titulo;

        public InsertaCategoriaAlGuardar(string cadenaConexion, string titulo)
        {
            _cadenaConexion = cadenaConexion;
            _titulo = titulo;
        }

        public bool Disparo { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!Disparo)
            {
                Disparo = true;

                await using var conexion = new SqliteConnection(_cadenaConexion);
                await conexion.OpenAsync(cancellationToken);

                var comando = conexion.CreateCommand();
                comando.CommandText =
                    "INSERT INTO Categoria (Titulo, Descripcion, Activa) VALUES ($titulo, 'carrera', 1);";
                comando.Parameters.AddWithValue("$titulo", _titulo);
                await comando.ExecuteNonQueryAsync(cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
