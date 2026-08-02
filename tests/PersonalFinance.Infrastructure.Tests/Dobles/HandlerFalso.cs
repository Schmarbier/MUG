using System.Net;
using System.Text;

namespace PersonalFinance.Infrastructure.Tests.Dobles;

/// <summary>
/// Doble de <see cref="HttpMessageHandler"/>: responde con cuerpos preparados y guarda lo que se
/// le pidió. Es lo que permite probar los adaptadores HTTP sin red y sin servicio levantado.
/// Las respuestas se consumen en orden; la última se repite si hay más llamadas que respuestas.
/// </summary>
internal sealed class HandlerFalso : HttpMessageHandler
{
    private readonly IReadOnlyList<(HttpStatusCode Codigo, string Cuerpo)> _respuestas;
    private readonly Exception? _falla;

    private HandlerFalso(IReadOnlyList<(HttpStatusCode, string)> respuestas, Exception? falla)
    {
        _respuestas = respuestas;
        _falla = falla;
    }

    /// <summary>Cuerpo de cada request recibida, en orden.</summary>
    public List<string> Pedidos { get; } = [];

    public int Llamadas => Pedidos.Count;

    public static HandlerFalso ConJson(params string[] cuerpos) =>
        new([.. cuerpos.Select(c => (HttpStatusCode.OK, c))], falla: null);

    public static HandlerFalso ConEstado(HttpStatusCode codigo, string cuerpo) =>
        new([(codigo, cuerpo)], falla: null);

    public static HandlerFalso QueFalla(Exception falla) => new([], falla);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Pedidos.Add(request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken));

        if (_falla is not null)
        {
            throw _falla;
        }

        var (codigo, cuerpo) = _respuestas[Math.Min(Pedidos.Count - 1, _respuestas.Count - 1)];

        return new HttpResponseMessage(codigo)
        {
            Content = new StringContent(cuerpo, Encoding.UTF8, "application/json"),
        };
    }
}
