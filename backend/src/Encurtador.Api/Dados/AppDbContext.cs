using Encurtador.Api.Cliques;
using Encurtador.Api.Links;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Dados;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();
    public DbSet<Click> Clicks => Set<Click>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Link>(link =>
        {
            link.Property(l => l.Codigo).HasMaxLength(Link.TamanhoMaximoCodigo);
            link.HasIndex(l => l.Codigo).IsUnique();
            link.Property(l => l.UrlOriginal).HasMaxLength(Link.TamanhoMaximoUrl);
        });

        modelBuilder.Entity<Click>(click =>
        {
            click.Property(c => c.Pais).HasMaxLength(2);
            click.Property(c => c.Navegador).HasMaxLength(Click.TamanhoMaximoNavegador);
            click.Property(c => c.Dispositivo).HasMaxLength(Click.TamanhoMaximoDispositivo);
            click.Property(c => c.Referer).HasMaxLength(Click.TamanhoMaximoReferer);

            // As estatísticas sempre filtram por link e agrupam por data.
            click.HasIndex(c => new { c.LinkId, c.CriadoEm });

            // Apagar um link apaga também os cliques dele.
            click.HasOne<Link>()
                .WithMany()
                .HasForeignKey(c => c.LinkId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
