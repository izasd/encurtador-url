using Encurtador.Api.Cliques;

namespace Encurtador.Tests.Cliques;

public class CliqueRegistradoTests
{
    [Fact]
    public void ParaClick_AnalisaOsDadosBrutosDaRequisicao()
    {
        var criadoEm = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
        var clique = new CliqueRegistrado(
            LinkId: 7,
            CriadoEm: criadoEm,
            UserAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1",
            Referer: "https://t.co/abc123",
            Pais: "br");

        var click = clique.ParaClick();

        Assert.Equal(7, click.LinkId);
        Assert.Equal(criadoEm, click.CriadoEm);
        Assert.Equal("Safari", click.Navegador);
        Assert.Equal("Celular", click.Dispositivo);
        Assert.Equal("t.co", click.Referer);
        Assert.Equal("BR", click.Pais);
    }

    [Fact]
    public void ParaClick_SemCabecalhos_UsaValoresPadrao()
    {
        var click = new CliqueRegistrado(7, DateTime.UtcNow, "", "", "").ParaClick();

        Assert.Equal(AnalisadorDeAcesso.Desconhecido, click.Navegador);
        Assert.Equal(AnalisadorDeAcesso.Desconhecido, click.Dispositivo);
        Assert.Null(click.Referer);
        Assert.Null(click.Pais);
    }
}
