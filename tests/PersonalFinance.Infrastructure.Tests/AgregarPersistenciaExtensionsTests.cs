using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PersonalFinance.Infrastructure.Tests;

public class AgregarPersistenciaExtensionsTests
{
    // Sad path del error documentado: una cadena vacía no es "usá la ruta por defecto", es un
    // error de configuración y tiene que romper al arrancar.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarPersistencia_CadenaVacia_LanzaArgumentException(string cadena)
    {
        var excepcion = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AgregarPersistencia(cadena));

        Assert.Equal("cadenaConexion", excepcion.ParamName);
    }

    // Sad path del error documentado: el directorio de datos no existe en la primera corrida.
    // Se crea antes de abrir la conexión; no es un error terminal.
    [Fact]
    public void AgregarPersistencia_DirectorioInexistente_LoCreaYAbreLaConexion()
    {
        var directorio = DirectorioTemporal();
        var rutaArchivo = Path.Combine(directorio, "personalfinance.db");

        try
        {
            Assert.False(Directory.Exists(directorio));

            new ServiceCollection().AgregarPersistencia($"Data Source={rutaArchivo}");

            Assert.True(Directory.Exists(directorio));
            Assert.True(File.Exists(rutaArchivo));
        }
        finally
        {
            Borrar(directorio);
        }
    }

    // Sad path del error documentado: si el archivo no es accesible, la SqliteException se
    // propaga al composition root. No se traga ni se convierte en otra cosa.
    [Fact]
    public void AgregarPersistencia_ArchivoNoAccesible_PropagaSqliteException()
    {
        var directorio = DirectorioTemporal();
        // La ruta apunta a un directorio, no a un archivo: SQLite no puede abrirlo.
        var rutaOcupada = Path.Combine(directorio, "personalfinance.db");
        Directory.CreateDirectory(rutaOcupada);

        try
        {
            Assert.Throws<SqliteException>(
                () => new ServiceCollection().AgregarPersistencia($"Data Source={rutaOcupada}"));
        }
        finally
        {
            Borrar(directorio);
        }
    }

    // Valida M-06: la base no está cifrada (riesgo aceptado R-01), así que los permisos del
    // sistema de archivos son su única defensa y se ponen a propósito, sin heredar nada.
    [Fact]
    public void AgregarPersistencia_CreaElArchivoConAclRestringidaAlUsuario()
    {
        var directorio = DirectorioTemporal();
        var rutaArchivo = Path.Combine(directorio, "personalfinance.db");

        try
        {
            new ServiceCollection().AgregarPersistencia($"Data Source={rutaArchivo}");

            if (OperatingSystem.IsWindows())
            {
                AssertAclSoloDelUsuarioActual(rutaArchivo);
            }
            else
            {
                AssertPermisosSoloDelUsuarioActual(rutaArchivo);
            }
        }
        finally
        {
            Borrar(directorio);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void AssertAclSoloDelUsuarioActual(string rutaArchivo)
    {
        using var identidad = WindowsIdentity.GetCurrent();
        var seguridad = new FileInfo(rutaArchivo).GetAccessControl();
        var reglas = seguridad.GetAccessRules(
            includeExplicit: true, includeInherited: true, targetType: typeof(SecurityIdentifier));

        Assert.NotEmpty(reglas);
        Assert.All(
            reglas.Cast<FileSystemAccessRule>(),
            regla => Assert.Equal(identidad.User, regla.IdentityReference));
    }

    [UnsupportedOSPlatform("windows")]
    private static void AssertPermisosSoloDelUsuarioActual(string rutaArchivo)
    {
        var permisos = File.GetUnixFileMode(rutaArchivo);
        var deGrupoYOtros = permisos & ~(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        Assert.Equal(UnixFileMode.None, deGrupoYOtros);
    }

    private static string DirectorioTemporal() =>
        Path.Combine(Path.GetTempPath(), $"personalfinance-tests-{Guid.NewGuid():N}");

    private static void Borrar(string directorio)
    {
        if (Directory.Exists(directorio))
        {
            Directory.Delete(directorio, recursive: true);
        }
    }
}
