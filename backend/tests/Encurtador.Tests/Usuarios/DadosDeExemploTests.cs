using Encurtador.Api.Cliques;
using Encurtador.Api.Usuarios;

namespace Encurtador.Tests.Usuarios;

public class DadosDeExemploTests
{
    private static readonly DateTime Desde = new(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Ate = Desde.AddDays(30);

    private static List<Click> Gerar(int quantidade = 2_000) =>
        DadosDeExemplo.GerarCliques(linkId: 7, Desde, Ate, quantidade, new Random(42));

    [Fact]
    public void GerarCliques_GeraAQuantidadePedidaParaOLink()
    {
        var cliques = Gerar(140);

        Assert.Equal(140, cliques.Count);
        Assert.All(cliques, c => Assert.Equal(7, c.LinkId));
    }

    [Fact]
    public void GerarCliques_FicamEntreACriacaoDoLinkEAgora()
    {
        Assert.All(Gerar(), c => Assert.InRange(c.CriadoEm, Desde, Ate));
    }

    [Fact]
    public void GerarCliques_ConcentramSeLogoDepoisDaCriacao()
    {
        var cliques = Gerar();
        var metade = Desde + (Ate - Desde) / 2;

        Assert.True(cliques.Count(c => c.CriadoEm < metade) > 3 * cliques.Count(c => c.CriadoEm >= metade));
    }

    [Fact]
    public void GerarCliques_UsamApenasValoresQueOAnalisadorDeAcessoProduz()
    {
        string[] navegadores = ["Chrome", "Safari", "Edge", "Firefox", "Samsung Internet", "Opera", AnalisadorDeAcesso.Robo];
        string[] dispositivos = ["Celular", "Computador", "Tablet", AnalisadorDeAcesso.Robo];

        Assert.All(Gerar(), c =>
        {
            Assert.Contains(c.Navegador, navegadores);
            Assert.Contains(c.Dispositivo, dispositivos);
            Assert.True(c.Pais is null or { Length: 2 });

            // Robô aparece nos dois campos ao mesmo tempo e nunca tem origem.
            Assert.Equal(c.Navegador == AnalisadorDeAcesso.Robo, c.Dispositivo == AnalisadorDeAcesso.Robo);
            if (c.Navegador == AnalisadorDeAcesso.Robo)
            {
                Assert.Null(c.Referer);
            }
        });
    }
}
