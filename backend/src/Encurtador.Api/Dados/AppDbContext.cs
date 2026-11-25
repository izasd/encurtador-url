using Encurtador.Api.Links;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Dados;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Link>(link =>
        {
            link.Property(l => l.Codigo).HasMaxLength(Link.TamanhoMaximoCodigo);
            link.HasIndex(l => l.Codigo).IsUnique();
            link.Property(l => l.UrlOriginal).HasMaxLength(Link.TamanhoMaximoUrl);
        });
    }
}
