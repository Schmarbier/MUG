using Microsoft.EntityFrameworkCore;
using PersonalFinance.Bot.Data;
using PersonalFinance.Bot.Domain;

namespace PersonalFinance.Bot.Services;

/// <summary>
/// Procesa los mensajes guardados: valida (RF-07), clasifica (RF-05), crea el movimiento
/// (RF-04) y marca el mensaje como procesado (RF-06); o lo marca con error y motivo (RF-07).
/// </summary>
public sealed class ProcesadorService
{
    private readonly PersonalFinanceContext _db;
    private readonly MensajeParser _parser;
    private readonly IClasificador _clasificador;

    public ProcesadorService(PersonalFinanceContext db, MensajeParser parser, IClasificador clasificador)
    {
        _db = db;
        _parser = parser;
        _clasificador = clasificador;
    }

    /// <summary>Procesa todos los mensajes pendientes (no procesados y sin error). Devuelve cuántos movimientos creó.</summary>
    public async Task<int> ProcesarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _db.Mensajes
            .Where(m => !m.Procesado && !m.Error)
            .ToListAsync(ct);

        if (pendientes.Count == 0)
            return 0;

        var categorias = await _db.Categorias.ToListAsync(ct);

        var creados = 0;
        foreach (var mensaje in pendientes)
        {
            if (await ProcesarAsync(mensaje, categorias, ct))
                creados++;
        }

        return creados;
    }

    /// <summary>
    /// Procesa un mensaje concreto. Reutilizable para reprocesar (RF-11): limpia el estado
    /// de error previo antes de intentar. Devuelve true si creó el movimiento.
    /// </summary>
    public async Task<bool> ProcesarAsync(
        Mensaje mensaje, IReadOnlyList<Categoria> categorias, CancellationToken ct = default)
    {
        var parseo = _parser.Parse(mensaje.Texto);
        if (!parseo.EsValido)
        {
            mensaje.Procesado = false;
            mensaje.Error = true;
            mensaje.Motivo = parseo.Motivo;
            await _db.SaveChangesAsync(ct);
            return false;
        }

        var clasificacion = await _clasificador.ClasificarAsync(
            parseo.Descripcion, parseo.Monto, categorias, ct);

        _db.Movimientos.Add(new Movimiento
        {
            MensajeId = mensaje.Id,
            CategoriaId = clasificacion.CategoriaId,
            Monto = parseo.Monto,
            Tipo = clasificacion.Tipo,
            Fecha = mensaje.FechaRecibido
        });

        mensaje.Procesado = true;
        mensaje.Error = false;
        mensaje.Motivo = null;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
