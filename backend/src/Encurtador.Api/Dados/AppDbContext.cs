using Encurtador.Api.Cliques;
using Encurtador.Api.Links;
using Encurtador.Api.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Dados;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Link> Links => Set<Link>();
    public DbSet<Click> Clicks => Set<Click>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(usuario =>
        {
            usuario.Property(u => u.Nome).HasMaxLength(User.TamanhoMaximoNome);
            usuario.Property(u => u.Email).HasMaxLength(User.TamanhoMaximoEmail);
            usuario.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Link>(link =>
        {
            link.Property(l => l.Codigo).HasMaxLength(Link.TamanhoMaximoCodigo);
            link.HasIndex(l => l.Codigo).IsUnique();
            link.Property(l => l.UrlOriginal).HasMaxLength(Link.TamanhoMaximoUrl);

            // Apagar um usuário apaga também os links dele (e, em cascata, os cliques).
            link.HasOne<User>()
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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
