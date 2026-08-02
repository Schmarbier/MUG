using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Domain.Puertos;
using PersonalFinance.Infrastructure.Persistencia;
using PersonalFinance.Infrastructure.Reloj;

namespace PersonalFinance.Infrastructure;

/// <summary>
/// Registro de la persistencia. El composition root sólo llama a este método: no registra
/// servicios sueltos ni conoce EF Core (AGENTS.md -> Code conventions).
/// </summary>
public static class AgregarPersistenciaExtensions
{
    private const string CarpetaDatos = "PersonalFinance";
    private const string NombreArchivo = "personalfinance.db";

    /// <summary>
    /// Registra el contexto, los tres repositorios, la unidad de trabajo, el reloj y el seed.
    /// Recibe la cadena de conexión como primitivo, nunca el objeto de configuración de la app:
    /// leer configuración es tarea exclusiva del composition root.
    /// </summary>
    /// <param name="cadenaConexion">
    /// Si es <c>null</c> se usa <c>%LOCALAPPDATA%\PersonalFinance\personalfinance.db</c>, ruta
    /// absoluta y estable que comparten Bot y Web.
    /// </param>
    public static IServiceCollection AgregarPersistencia(
        this IServiceCollection servicios,
        string? cadenaConexion = null)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        if (cadenaConexion is not null && string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new ArgumentException(
                "La cadena de conexión no puede estar vacía.", nameof(cadenaConexion));
        }

        var cadena = cadenaConexion ?? CadenaPorDefecto();

        // Se prepara acá, al arrancar, y no en la primera consulta: si el archivo no se puede
        // crear o abrir, el proceso tiene que fallar ahora y no a mitad de una corrida.
        PrepararAlmacenamiento(cadena);

        servicios.AddDbContext<PersonalFinanceDbContext>(opciones => opciones.UseSqlite(cadena));

        // Scoped: los tres repositorios y la unidad de trabajo comparten el mismo contexto
        // dentro del scope de la corrida. Sin eso, ConfirmarAsync no confirmaría lo que los
        // repositorios agregaron.
        servicios.AddScoped<IRepositorioMensajes, RepositorioMensajesEfCore>();
        servicios.AddScoped<IRepositorioCategorias, RepositorioCategoriasEfCore>();
        servicios.AddScoped<IRepositorioMovimientos, RepositorioMovimientosEfCore>();
        servicios.AddScoped<IUnitOfWork, UnitOfWorkEfCore>();
        servicios.AddScoped<SeedCategorias>();

        servicios.AddSingleton<IReloj, RelojSistema>();

        return servicios;
    }

    private static string CadenaPorDefecto()
    {
        var carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            CarpetaDatos);

        return new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(carpeta, NombreArchivo),
        }.ToString();
    }

    /// <summary>
    /// Crea el directorio y el archivo con permisos restringidos al usuario actual y verifica
    /// que la conexión abra. M-06 del threat model: la base no está cifrada (riesgo aceptado
    /// R-01), así que los permisos del sistema de archivos son su única defensa y tienen que
    /// estar puestos a propósito, no heredados del default del sistema.
    /// </summary>
    private static void PrepararAlmacenamiento(string cadenaConexion)
    {
        var constructor = new SqliteConnectionStringBuilder(cadenaConexion);

        // Una base en memoria no tiene archivo que proteger.
        if (constructor.Mode == SqliteOpenMode.Memory ||
            string.IsNullOrWhiteSpace(constructor.DataSource) ||
            constructor.DataSource == ":memory:")
        {
            return;
        }

        var rutaArchivo = Path.GetFullPath(constructor.DataSource);
        var directorio = Path.GetDirectoryName(rutaArchivo);

        if (!string.IsNullOrEmpty(directorio))
        {
            Directory.CreateDirectory(directorio);
            RestringirDirectorio(directorio);
        }

        // Abrir crea el archivo si no existe y propaga SqliteException si no es accesible.
        // Con Pooling desactivado: Microsoft.Data.Sqlite poolea las conexiones, así que sin esto
        // el Dispose devolvería la conexión al pool y el archivo quedaría tomado por el proceso
        // antes de que nadie lo haya pedido.
        var cadenaVerificacion = new SqliteConnectionStringBuilder(cadenaConexion)
        {
            Pooling = false,
        }.ToString();

        using (var conexion = new SqliteConnection(cadenaVerificacion))
        {
            conexion.Open();
        }

        RestringirArchivo(rutaArchivo);
    }

    private static void RestringirDirectorio(string directorio)
    {
        if (OperatingSystem.IsWindows())
        {
            RestringirDirectorioWindows(directorio);
        }
        else
        {
            RestringirUnix(directorio, esDirectorio: true);
        }
    }

    private static void RestringirArchivo(string rutaArchivo)
    {
        if (OperatingSystem.IsWindows())
        {
            RestringirArchivoWindows(rutaArchivo);
        }
        else
        {
            RestringirUnix(rutaArchivo, esDirectorio: false);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void RestringirDirectorioWindows(string directorio)
    {
        var usuario = UsuarioActual();
        if (usuario is null)
        {
            return;
        }

        var seguridad = new DirectorySecurity();
        seguridad.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        seguridad.AddAccessRule(new FileSystemAccessRule(
            usuario,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));

        new DirectoryInfo(directorio).SetAccessControl(seguridad);
    }

    [SupportedOSPlatform("windows")]
    private static void RestringirArchivoWindows(string rutaArchivo)
    {
        var usuario = UsuarioActual();
        if (usuario is null)
        {
            return;
        }

        var seguridad = new FileSecurity();
        seguridad.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        seguridad.AddAccessRule(new FileSystemAccessRule(
            usuario,
            FileSystemRights.FullControl,
            AccessControlType.Allow));

        new FileInfo(rutaArchivo).SetAccessControl(seguridad);
    }

    [SupportedOSPlatform("windows")]
    private static SecurityIdentifier? UsuarioActual()
    {
        using var identidad = WindowsIdentity.GetCurrent();
        return identidad.User;
    }

    [UnsupportedOSPlatform("windows")]
    private static void RestringirUnix(string ruta, bool esDirectorio)
    {
        var permisos = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        if (esDirectorio)
        {
            permisos |= UnixFileMode.UserExecute;
        }

        File.SetUnixFileMode(ruta, permisos);
    }
}
