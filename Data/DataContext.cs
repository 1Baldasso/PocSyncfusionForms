using Domain;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Campo> Campos { get; set; }
    public DbSet<Documento> Documentos { get; set; }
}