using System.Net;
using Encurtador.Api.Cliques;
using Encurtador.Api.Links;
using Encurtador.IntegrationTests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.IntegrationTests;

[Collection(ColecaoDaApi.Nome)]
public class RedirecionamentoTests(ApiFactory api)
{
    private const string UserAgentDeIphone =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1";

    [Fact]
    public async Task Redirecionar_LinkDisponivel_Retorna302ERegistraOCliqueComOsDadosDoAcesso()
    {
        var dono = await api.CriarUsuarioAsync();
        var link = await dono.CriarLinkAsync("https://exemplo.com/destino");

        var acesso = new HttpRequestMessage(HttpMethod.Get, $"/{link.Codigo}");
        acesso.Headers.TryAddWithoutValidation("User-Agent", UserAgentDeIphone);
        acesso.Headers.Referrer = new Uri("https://t.co/abc123");
        acesso.Headers.Add("CF-IPCountry", "br");
        var resposta = await api.CriarCliente().SendAsync(acesso);

        Assert.Equal(HttpStatusCode.Found, resposta.StatusCode);
        Assert.Equal(new Uri("https://exemplo.com/destino"), resposta.Headers.Location);

        var estatisticas = await dono.AguardarCliquesAsync(link.Codigo, total: 1);
        Assert.Equal(1, estatisticas.TotalDeCliques);
        Assert.Equal(new CliquesPorCategoria("Safari", 1), Assert.Single(estatisticas.PorNavegador));
        Assert.Equal(new CliquesPorCategoria("Celular", 1), Assert.Single(estatisticas.PorDispositivo));
        Assert.Equal(new CliquesPorCategoria("t.co", 1), Assert.Single(estatisticas.PorReferer));
        Assert.Equal(new CliquesPorCategoria("BR", 1), Assert.Single(estatisticas.PorPais));
    }

    [Fact]
    public async Task Redirecionar_CodigoInexistente_Retorna404()
    {
        var codigo = ClienteDaApi.CodigoUnico();

        var problema = await ClienteDaApi.LerProblemaAsync(await api.CriarCliente().GetAsync($"/{codigo}"), HttpStatusCode.NotFound);

        Assert.Equal($"Nenhum link encontrado com o código '{codigo}'.", problema.Detail);
    }

    [Fact]
    public async Task Redirecionar_LinkDesativado_Retorna410SemContarOClique()
    {
        var dono = await api.CriarUsuarioAsync();
        var link = await dono.CriarLinkAsync();
        await dono.AtualizarLinkAsync(link.Codigo, """{"ativo":false}""");

        var problema = await ClienteDaApi.LerProblemaAsync(await api.CriarCliente().GetAsync($"/{link.Codigo}"), HttpStatusCode.Gone);
        Assert.Equal("Este link foi desativado pelo dono.", problema.Detail);

        // Reativado, o link volta a redirecionar. Os cliques são gravados na ordem em que chegam:
        // quando o clique marcado aparecer, um clique do acesso bloqueado já teria aparecido também.
        await dono.AtualizarLinkAsync(link.Codigo, """{"ativo":true}""");
        var marcado = new HttpRequestMessage(HttpMethod.Get, $"/{link.Codigo}") { Headers = { Referrer = new Uri("https://marcador.exemplo") } };
        Assert.Equal(HttpStatusCode.Found, (await api.CriarCliente().SendAsync(marcado)).StatusCode);

        var estatisticas = await dono.AguardarEstatisticasAsync(link.Codigo, e => e.PorReferer.Any(r => r.Nome == "marcador.exemplo"));
        Assert.Equal(1, estatisticas.TotalDeCliques);
    }

    [Fact]
    public async Task Redirecionar_LinkExpirado_Retorna410()
    {
        var dono = await api.CriarUsuarioAsync();
        var link = await dono.CriarLinkAsync(expiraEm: DateTimeOffset.UtcNow.AddDays(1).ToString("O"));

        // A API só aceita datas futuras; o tempo "passa" direto no banco.
        await using (var db = api.CriarDbContext())
        {
            await db.Links
                .Where(l => l.Codigo == link.Codigo)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.ExpiraEm, DateTime.UtcNow.AddMinutes(-1)));
        }

        var problema = await ClienteDaApi.LerProblemaAsync(await api.CriarCliente().GetAsync($"/{link.Codigo}"), HttpStatusCode.Gone);
        Assert.Equal("Este link expirou.", problema.Detail);

        var links = await ClienteDaApi.LerAsync<List<LinkResponse>>(await dono.GetAsync("/api/links"), HttpStatusCode.OK);
        Assert.Equal(SituacaoDoLink.Expirado, Assert.Single(links).Situacao);
    }
}
