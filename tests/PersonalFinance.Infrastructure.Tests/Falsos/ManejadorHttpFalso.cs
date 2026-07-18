namespace PersonalFinance.Infrastructure.Tests.Falsos;

/// <summary>Simula el servidor de Ollama a nivel de transporte HTTP (contracts/clasificador.md § Verificación).</summary>
public sealed class ManejadorHttpFalso(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        responder(request, cancellationToken);

    public static HttpResponseMessage RespuestaGenerate(string responseJson) => new(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(
            $$"""{"model":"llama3.1","created_at":"2026-01-01T00:00:00Z","response":{{System.Text.Json.JsonSerializer.Serialize(responseJson)}},"done":true}""",
            System.Text.Encoding.UTF8,
            "application/json")
    };
}
