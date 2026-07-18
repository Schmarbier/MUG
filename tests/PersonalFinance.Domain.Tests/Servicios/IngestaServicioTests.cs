using PersonalFinance.Domain.Servicios;
using PersonalFinance.Domain.Tests.Falsos;

namespace PersonalFinance.Domain.Tests.Servicios;

public sealed class IngestaServicioTests
{
    [Fact]
    public async Task Mensaje_de_chat_distinto_al_autorizado_se_descarta_sin_guardarse()
    {
        var mensajes = new RepositorioMensajeFalso();
        var servicio = new IngestaServicio(mensajes, chatAutorizado: 100L);

        var resultado = await servicio.IngerirAsync(
            chatId: 999L,
            identificadorCanal: 1L,
            texto: "2000 en super",
            fechaRecepcionUtc: DateTimeOffset.UtcNow);

        Assert.Null(resultado);
        Assert.Empty(mensajes.Mensajes);
    }

    [Fact]
    public async Task Mensaje_del_chat_autorizado_se_guarda()
    {
        var mensajes = new RepositorioMensajeFalso();
        var servicio = new IngestaServicio(mensajes, chatAutorizado: 100L);

        var resultado = await servicio.IngerirAsync(
            chatId: 100L,
            identificadorCanal: 1L,
            texto: "2000 en super",
            fechaRecepcionUtc: DateTimeOffset.UtcNow);

        Assert.NotNull(resultado);
        Assert.Single(mensajes.Mensajes);
    }
}
