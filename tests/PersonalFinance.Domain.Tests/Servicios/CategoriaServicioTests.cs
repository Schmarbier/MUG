using PersonalFinance.Domain.Servicios;
using PersonalFinance.Domain.Tests.Falsos;

namespace PersonalFinance.Domain.Tests.Servicios;

public sealed class CategoriaServicioTests
{
    private readonly RepositorioCategoriaFalso _categorias = new();
    private readonly CategoriaServicio _servicio;

    public CategoriaServicioTests()
    {
        _servicio = new CategoriaServicio(_categorias);
    }

    [Fact]
    public async Task Crear_con_titulo_unico_la_deja_activa_con_ese_titulo_y_descripcion()
    {
        var categoria = await _servicio.CrearAsync("Hogar", "gastos del hogar");

        Assert.Equal("Hogar", categoria.Titulo);
        Assert.Equal("gastos del hogar", categoria.Descripcion);
        Assert.True(categoria.Activa);
    }

    [Fact]
    public async Task Crear_con_titulo_ya_existente_se_rechaza_con_error()
    {
        await _servicio.CrearAsync("Hogar", "gastos del hogar");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.CrearAsync("Hogar", "otra descripción"));
    }

    [Fact]
    public async Task Editar_con_titulo_ya_existente_se_rechaza_con_error_tambien_contra_desactivadas()
    {
        await _servicio.CrearAsync("Ocio", "gastos de ocio");
        var otra = await _servicio.CrearAsync("Trabajo", "gastos de trabajo");
        await _servicio.EliminarAsync(otra.Id); // sin movimientos -> se borra, así que forzamos desactivación:
        var desactivada = await _servicio.CrearAsync("Trabajo", "gastos de trabajo");
        _categorias.TieneMovimientosPorCategoria.Add(desactivada.Id);
        await _servicio.EliminarAsync(desactivada.Id); // ahora sí se desactiva

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.EditarAsync(desactivada.Id, "Ocio", "x"));
    }

    [Fact]
    public async Task Editar_titulo_lo_actualiza()
    {
        var categoria = await _servicio.CrearAsync("Hogar", "gastos del hogar");

        await _servicio.EditarAsync(categoria.Id, "Casa", categoria.Descripcion);

        var actualizada = await _categorias.ObtenerPorIdAsync(categoria.Id);
        Assert.Equal("Casa", actualizada!.Titulo);
    }

    [Fact]
    public async Task Eliminar_sin_movimientos_la_borra()
    {
        var categoria = await _servicio.CrearAsync("Hogar", "gastos del hogar");

        await _servicio.EliminarAsync(categoria.Id);

        Assert.Null(await _categorias.ObtenerPorIdAsync(categoria.Id));
    }

    [Fact]
    public async Task Eliminar_con_movimientos_la_desactiva_en_lugar_de_borrarla()
    {
        var categoria = await _servicio.CrearAsync("Hogar", "gastos del hogar");
        _categorias.TieneMovimientosPorCategoria.Add(categoria.Id);

        await _servicio.EliminarAsync(categoria.Id);

        var resultado = await _categorias.ObtenerPorIdAsync(categoria.Id);
        Assert.NotNull(resultado);
        Assert.False(resultado!.Activa);
    }

    [Fact]
    public async Task Editar_titulo_o_descripcion_de_categoria_desactivada_conserva_su_estado()
    {
        var categoria = await _servicio.CrearAsync("Ocio", "gastos de ocio");
        _categorias.TieneMovimientosPorCategoria.Add(categoria.Id);
        await _servicio.EliminarAsync(categoria.Id); // queda desactivada

        await _servicio.EditarAsync(categoria.Id, "Entretenimiento", "nueva descripción");

        var actualizada = await _categorias.ObtenerPorIdAsync(categoria.Id);
        Assert.Equal("Entretenimiento", actualizada!.Titulo);
        Assert.False(actualizada.Activa);
    }

    [Fact]
    public async Task Reactivar_una_categoria_desactivada_la_vuelve_disponible()
    {
        var categoria = await _servicio.CrearAsync("Ocio", "gastos de ocio");
        _categorias.TieneMovimientosPorCategoria.Add(categoria.Id);
        await _servicio.EliminarAsync(categoria.Id);

        await _servicio.ReactivarAsync(categoria.Id);

        var actualizada = await _categorias.ObtenerPorIdAsync(categoria.Id);
        Assert.True(actualizada!.Activa);
    }
}
