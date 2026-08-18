using System.Net.Sockets;

namespace PersonalFinance.Infrastructure.Tests.Integracion;

/// <summary>
/// Guarda de los tests de integración. Existe para que "Ollama no está levantado" se distinga de
/// "el clasificador anda mal": sin esto, el test fallaría con un timeout opaco quince segundos
/// por mensaje y nadie sabría qué mirar.
/// </summary>
internal static class OllamaDisponible
{
    public static async Task AsegurarAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var conexion = new TcpClient();

        try
        {
            await conexion.ConnectAsync(uri.Host, uri.Port, cancellationToken);
        }
        catch (Exception excepcion) when (excepcion is SocketException or OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Ollama no responde en {uri} — levantalo con `ollama serve` y asegurate de " +
                "tener el modelo con `ollama pull llama3.1`.");
        }
    }
}
