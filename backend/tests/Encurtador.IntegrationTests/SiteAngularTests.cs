using System.Net;
using Encurtador.IntegrationTests.Infraestrutura;
using Microsoft.AspNetCore.Hosting;

namespace Encurtador.IntegrationTests;

[Collection(ColecaoDaApi.Nome)]
public sealed class SiteAngularTests(ApiFactory api) : IDisposable
{
    private readonly DirectoryInfo _wwwroot = CriarSitePublicado();

    [Fact]
    public async Task ComOSitePublicado_ServeOSiteEmAppEMantemOsLinksCurtosNaRaiz()
    {
        await using var site = api.WithWebHostBuilder(host => host.UseSetting(WebHostDefaults.WebRootKey, _wwwroot.FullName));
        var cliente = site.CriarCliente();

        var raiz = await cliente.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, raiz.StatusCode);
        Assert.Equal("/app/", raiz.Headers.Location?.OriginalString);

        // A entrada do site e as rotas do Angular (inclusive ao recarregar a página) devolvem
        // o index.html, que o navegador sempre revalida.
        foreach (var caminho in new[] { "/app", "/app/", "/app/links/abc123/estatisticas" })
        {
            var pagina = await cliente.GetAsync(caminho);
            Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
            Assert.Equal("text/html", pagina.Content.Headers.ContentType?.MediaType);
            Assert.Contains("<app-root>", await pagina.Content.ReadAsStringAsync());
            Assert.True(pagina.Headers.CacheControl?.NoCache);
        }

        var script = await cliente.GetAsync("/app/main-ABCD1234.js");
        Assert.Equal(HttpStatusCode.OK, script.StatusCode);
        Assert.Equal(TimeSpan.FromDays(365), script.Headers.CacheControl?.MaxAge);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/app/chunk-NAOEXISTE.js")).StatusCode);

        var dono = await site.CriarUsuarioAsync();
        var link = await dono.CriarLinkAsync("https://exemplo.com/destino");
        Assert.Equal(HttpStatusCode.Found, (await cliente.GetAsync($"/{link.Codigo}")).StatusCode);
    }

    [Fact]
    public async Task SemOSitePublicado_SoAApiResponde()
    {
        var cliente = api.CriarCliente();

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/app/")).StatusCode);
    }

    public void Dispose() => _wwwroot.Delete(recursive: true);

    private static DirectoryInfo CriarSitePublicado()
    {
        var wwwroot = Directory.CreateTempSubdirectory("encurtador-wwwroot-");
        var pasta = wwwroot.CreateSubdirectory("app");
        File.WriteAllText(Path.Combine(pasta.FullName, "index.html"), """<!doctype html><base href="/app/"><app-root></app-root>""");
        File.WriteAllText(Path.Combine(pasta.FullName, "main-ABCD1234.js"), "console.log('ok');");
        return wwwroot;
    }
}
