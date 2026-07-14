namespace PersonalFinance.Domain;

public class Categoria
{
    public int Id { get; set; }
    public string Titulo { get; set; } = default!;
    public string Descripcion { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;
}
