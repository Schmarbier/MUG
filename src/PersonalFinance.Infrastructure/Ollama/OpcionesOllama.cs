namespace PersonalFinance.Infrastructure.Ollama;

/// <summary>
/// Configuración del clasificador, ya validada por <c>AgregarClasificador</c>.
/// </summary>
public sealed record OpcionesOllama(Uri Uri, string Modelo)
{
    /// <summary>
    /// M-02 (threat model): loopback por defecto. El texto de los mensajes es PII financiera y
    /// viaja en HTTP plano; mientras no salga de la máquina, es aceptable.
    /// </summary>
    public static readonly Uri UriPorDefecto = new("http://127.0.0.1:11434");

    public const string ModeloPorDefecto = "llama3.1";

    /// <summary>
    /// 15 s, deliberadamente por encima del umbral de 5 s de NFR-02. Si el timeout fuera 5 s,
    /// toda respuesta lenta se convertiría en <c>NoDisponible</c> y saldría de la muestra de
    /// latencia, con lo cual el p90 no podría fallar nunca. Con 15 s, un mensaje que tarda 12 s
    /// se clasifica bien <b>y</b> hace fallar el test de latencia, que es lo que se busca.
    /// </summary>
    public static readonly TimeSpan TimeoutPorDefecto = TimeSpan.FromSeconds(15);

    public TimeSpan Timeout { get; init; } = TimeoutPorDefecto;
}
