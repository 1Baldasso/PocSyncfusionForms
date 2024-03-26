namespace Domain;

public class Documento
{
    public Guid Id { get; set; }
    public string Nome { get; set; }
    public string Path { get; set; }
    public ICollection<Campo> Campos { get; set; }
}