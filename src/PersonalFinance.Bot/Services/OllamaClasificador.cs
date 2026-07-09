using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OllamaSharp;
using OllamaSharp.Models;
using PersonalFinance.Bot.Domain;

namespace PersonalFinance.Bot.Services;

/// <summary>
/// Clasificador real basado en un modelo local vía Ollama (RF-05). Arma el prompt con las
/// instrucciones (Prompts/clasificador.md) + las categorías + el movimiento, pide al modelo
/// una respuesta JSON e interpreta esa respuesta contra las categorías existentes.
/// </summary>
public sealed class OllamaClasificador : IClasificador
{
    private readonly IOllamaApiClient _ollama;
    private readonly string _instrucciones;

    public OllamaClasificador(IOllamaApiClient ollama, string instrucciones)
    {
        _ollama = ollama;
        _instrucciones = instrucciones;
    }

    public async Task<ResultadoClasificacion> ClasificarAsync(
        string descripcion, decimal monto, IReadOnlyList<Categoria> categorias, CancellationToken ct = default)
    {
        var prompt = ConstruirPrompt(_instrucciones, descripcion, monto, categorias);

        var request = new GenerateRequest { Prompt = prompt, Stream = false, Format = "json" };

        var sb = new StringBuilder();
        await foreach (var parte in _ollama.GenerateAsync(request, ct))
            sb.Append(parte?.Response);

        return Interpretar(sb.ToString(), categorias);
    }

    internal static string ConstruirPrompt(
        string instrucciones, string descripcion, decimal monto, IReadOnlyList<Categoria> categorias)
    {
        var sb = new StringBuilder(instrucciones);
        sb.AppendLine().AppendLine().AppendLine("## Categorías disponibles");
        foreach (var c in categorias)
            sb.AppendLine($"- {c.Titulo}: {c.Descripcion}");
        sb.AppendLine().AppendLine("## Movimiento a clasificar");
        sb.AppendLine($"- Descripción: {descripcion}");
        sb.AppendLine($"- Monto: {monto}");
        return sb.ToString();
    }

    /// <summary>
    /// Interpreta la respuesta JSON del modelo y la resuelve contra las categorías reales.
    /// Tolerante: si el título no matchea exacto intenta por coincidencia; si nada matchea
    /// cae en "Otros" (o la última categoría). Método puro: testeable sin Ollama.
    /// </summary>
    internal static ResultadoClasificacion Interpretar(string respuesta, IReadOnlyList<Categoria> categorias)
    {
        if (categorias.Count == 0)
            throw new InvalidOperationException("No hay categorías para clasificar.");

        var (categoriaTexto, tipoTexto) = ExtraerCampos(respuesta);

        var categoria =
            categorias.FirstOrDefault(c => string.Equals(c.Titulo, categoriaTexto, StringComparison.OrdinalIgnoreCase))
            ?? categorias.FirstOrDefault(c =>
                   categoriaTexto.Contains(c.Titulo, StringComparison.OrdinalIgnoreCase))
            ?? categorias.FirstOrDefault(c => c.Titulo.Equals("Otros", StringComparison.OrdinalIgnoreCase))
            ?? categorias[^1];

        var tipo = tipoTexto.Contains("ingres", StringComparison.OrdinalIgnoreCase)
            ? TipoMovimiento.Ingreso
            : TipoMovimiento.Egreso;

        return new ResultadoClasificacion(categoria.Id, tipo);
    }

    private static (string categoria, string tipo) ExtraerCampos(string respuesta)
    {
        // El modelo puede envolver el JSON en texto: tomamos el primer objeto {...}.
        var match = Regex.Match(respuesta ?? "", @"\{.*\}", RegexOptions.Singleline);
        if (!match.Success)
            return ("", "");

        try
        {
            using var doc = JsonDocument.Parse(match.Value);
            var root = doc.RootElement;
            var categoria = root.TryGetProperty("categoria", out var c) ? c.GetString() ?? "" : "";
            var tipo = root.TryGetProperty("tipo", out var t) ? t.GetString() ?? "" : "";
            return (categoria, tipo);
        }
        catch (JsonException)
        {
            return ("", "");
        }
    }
}
