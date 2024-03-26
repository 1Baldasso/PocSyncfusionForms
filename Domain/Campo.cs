namespace Domain;

public class Campo
{
    public Guid Id { get; set; }
    public string Key { get; set; }
    public string? Value { get; set; }
    public Documento Documento { get; set; }
    public Guid DocumentoId { get; set; }
}