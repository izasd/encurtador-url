using System.Net;
using System.Net.Http.Json;
using Encurtador.IntegrationTests.Infraestrutura;
using Microsoft.AspNetCore.Hosting;

namespace Encurtador.IntegrationTests;

[Collection(ColecaoDaApi.Nome)]
public class ProxyReversoTests(ApiFactory api)
{
    [Fact]
    public async Task SemProxyConfigurado_IgnoraOsCabecalhosRepassados()
    {
        var cliente = await api.CriarUsuarioAsync();
        cliente.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        Assert.StartsWith("http://localhost/", (await cliente.CriarLinkAsync()).UrlCurta);

        // Trocar o X-Forwarded-For a cada tentativa não escapa do limite do IP de verdade.
        var tentativas = api.CriarCliente();
        var respostas = new List<HttpStatusCode>();
        for (var i = 1; i <= 11; i++)
        {
            respostas.Add(await TentarLoginAsync(tentativas, "X-Forwarded-For", $"203.0.113.{i}"));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, respostas[^1]);
    }

    [Fact]
    public async Task ComProxy_UsaOEsquemaEOIpRepassados()
    {
        await using var comProxy = api.WithWebHostBuilder(host => host.UseSetting("ProxyReverso:Habilitado", "true"));

        var cliente = await comProxy.CriarUsuarioAsync();
        cliente.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        Assert.StartsWith("https://localhost/", (await cliente.CriarLinkAsync()).UrlCurta);

        // Tudo chega pela mesma conexão (a do proxy), mas cada visitante tem o próprio limite.
        var proxy = comProxy.CriarCliente();
        await EsgotarLimiteDeLoginAsync(proxy, "X-Forwarded-For", "198.51.100.7");
        Assert.Equal(HttpStatusCode.Unauthorized, await TentarLoginAsync(proxy, "X-Forwarded-For", "198.51.100.8"));

        // Um IP inventado pelo visitante fica à esquerda do que o proxy acrescentou e é ignorado.
        Assert.Equal(HttpStatusCode.TooManyRequests, await TentarLoginAsync(proxy, "X-Forwarded-For", "192.0.2.1, 198.51.100.7"));
    }

    [Fact]
    public async Task ComProxyDeDoisSaltosECabecalhoProprio_LeOIpNoLugarCerto()
    {
        await using var doisSaltos = api.WithWebHostBuilder(host =>
        {
            host.UseSetting("ProxyReverso:Habilitado", "true");
            host.UseSetting("ProxyReverso:Saltos", "2");
        });
        var proxy = doisSaltos.CriarCliente();
        await EsgotarLimiteDeLoginAsync(proxy, "X-Forwarded-For", "198.51.100.20, 172.16.0.1");
        Assert.Equal(HttpStatusCode.TooManyRequests, await TentarLoginAsync(proxy, "X-Forwarded-For", "192.0.2.1, 198.51.100.20, 172.16.0.2"));

        await using var cabecalhoProprio = api.WithWebHostBuilder(host =>
        {
            host.UseSetting("ProxyReverso:Habilitado", "true");
            host.UseSetting("ProxyReverso:CabecalhoDoIp", "CF-Connecting-IP");
        });
        proxy = cabecalhoProprio.CriarCliente();
        await EsgotarLimiteDeLoginAsync(proxy, "CF-Connecting-IP", "198.51.100.30");
        Assert.Equal(HttpStatusCode.Unauthorized, await TentarLoginAsync(proxy, "CF-Connecting-IP", "198.51.100.31"));
    }

    private static async Task EsgotarLimiteDeLoginAsync(HttpClient cliente, string cabecalho, string valor)
    {
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await TentarLoginAsync(cliente, cabecalho, valor));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, await TentarLoginAsync(cliente, cabecalho, valor));
    }

    private static async Task<HttpStatusCode> TentarLoginAsync(HttpClient cliente, string cabecalho, string valor)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = "ninguem@teste.com", senha = "senha-errada-123" }),
        };
        requisicao.Headers.Add(cabecalho, valor);

        return (await cliente.SendAsync(requisicao)).StatusCode;
    }
}
