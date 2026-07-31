using PersonalFinance.Domain.Entidades;
using Xunit;

namespace PersonalFinance.Domain.Tests;

public class MensajeTests
{
    private static readonly DateTime FechaRecepcion = new(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);

    private static Mensaje CrearMensaje() =>
        new(messageId: 42, texto: "$10.000 sueldo de julio", fechaRecepcion: FechaRecepcion);

    // Sustenta AC-01: el mensaje ingerido se guarda con su message_id, su texto original y su
    // fecha de recepción. FR-03 exige conservar el texto tal cual llegó.
    [Fact]
    public void Constructor_DatosValidos_ConservaLosDatos()
    {
        Mensaje mensaje = new(messageId: 42, texto: "$10.000 sueldo de julio", fechaRecepcion: FechaRecepcion);

        Assert.Equal(
            (42L, "$10.000 sueldo de julio", FechaRecepcion),
            (mensaje.MessageId, mensaje.Texto, mensaje.FechaRecepcion));
    }

    // Sustenta AC-06: el mensaje del que se creó un movimiento queda con procesado = true.
    [Fact]
    public void MarcarProcesado_MensajeNuevo_QuedaProcesadoTrue()
    {
        Mensaje mensaje = CrearMensaje();

        mensaje.MarcarProcesado();

        Assert.True(mensaje.Procesado);
    }

    // Sustenta AC-09, AC-10 y AC-11: el mensaje que no puede convertirse queda con error = true
    // y su motivo legible.
    [Fact]
    public void MarcarError_ConMotivo_QuedaErrorTrueYConMotivo()
    {
        Mensaje mensaje = CrearMensaje();

        mensaje.MarcarError("no contiene monto");

        Assert.True(mensaje.Error);
        Assert.Equal("no contiene monto", mensaje.Motivo);
    }

    // Sad path de AC-09/AC-10/AC-11: marcar error sin motivo es un error de programación,
    // no un camino del PRD.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarcarError_MotivoVacio_LanzaArgumentException(string motivo)
    {
        Mensaje mensaje = CrearMensaje();

        ArgumentException excepcion = Assert.Throws<ArgumentException>(() => mensaje.MarcarError(motivo));

        Assert.Equal("motivo", excepcion.ParamName);
    }

    // Sad path de AC-09/AC-10/AC-11: el intento fallido no deja el mensaje a medio marcar.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarcarError_MotivoVacio_NoMarcaElMensajeConError(string motivo)
    {
        Mensaje mensaje = CrearMensaje();

        Assert.Throws<ArgumentException>(() => mensaje.MarcarError(motivo));

        Assert.False(mensaje.Error);
    }

    // Sad path de AC-09/AC-10/AC-11: el motivo es un texto corto y legible, no un volcado.
    [Fact]
    public void MarcarError_MotivoMayorA200_LanzaArgumentOutOfRangeException()
    {
        Mensaje mensaje = CrearMensaje();
        string motivo = new('m', Mensaje.MotivoMaximo + 1);

        ArgumentOutOfRangeException excepcion =
            Assert.Throws<ArgumentOutOfRangeException>(() => mensaje.MarcarError(motivo));

        Assert.Equal("motivo", excepcion.ParamName);
    }

    // Sad path de AC-06 vs AC-09/AC-10/AC-11: procesado y error son estados excluyentes.
    [Fact]
    public void MarcarProcesado_MensajeYaConError_LanzaInvalidOperationException()
    {
        Mensaje mensaje = CrearMensaje();
        mensaje.MarcarError("tipo no reconocido");

        InvalidOperationException excepcion =
            Assert.Throws<InvalidOperationException>(mensaje.MarcarProcesado);

        Assert.Contains("error", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    // Sad path de AC-06: el mensaje en error sigue sin estar procesado después del intento.
    [Fact]
    public void MarcarProcesado_MensajeYaConError_NoQuedaProcesado()
    {
        Mensaje mensaje = CrearMensaje();
        mensaje.MarcarError("tipo no reconocido");

        Assert.Throws<InvalidOperationException>(mensaje.MarcarProcesado);

        Assert.False(mensaje.Procesado);
    }

    // Sad path de AC-01: un mensaje sin texto no se guarda; FR-03 conserva el texto original,
    // así que un texto vacío no es un mensaje válido.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_TextoVacio_LanzaArgumentException(string texto)
    {
        ArgumentException excepcion = Assert.Throws<ArgumentException>(
            () => new Mensaje(messageId: 42, texto: texto, fechaRecepcion: FechaRecepcion));

        Assert.Equal("texto", excepcion.ParamName);
    }

    // Sad path de AC-01: 4096 es el límite de un mensaje de Telegram; el texto ya viene truncado
    // desde la ingesta, así que un texto más largo es un error de programación.
    [Fact]
    public void Constructor_TextoMayorA4096_LanzaArgumentOutOfRangeException()
    {
        string texto = new('t', Mensaje.TextoMaximo + 1);

        ArgumentOutOfRangeException excepcion = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Mensaje(messageId: 42, texto: texto, fechaRecepcion: FechaRecepcion));

        Assert.Equal("texto", excepcion.ParamName);
    }
}
