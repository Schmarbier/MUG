namespace PersonalFinance.Domain.Entidades;

public class Categoria
{
    public int Id { get; set; }
    public required string Titulo { get; set; }
    public required string Descripcion { get; set; }
    public bool Activa { get; set; }
}
