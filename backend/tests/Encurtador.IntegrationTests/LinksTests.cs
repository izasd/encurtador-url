using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Links;
using Encurtador.IntegrationTests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.IntegrationTests;

[Collection(ColecaoDaApi.Nome)]
public class LinksTests(ApiFactory api)
{
    [Fact]
    public async Task Criar_SemCodigo_GeraUmCodigoERetorna201ComOEnderecoDoLinkCurto()
    {
        var cliente = await api.CriarUsuarioAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/links", new { urlOriginal = "https://exemplo.com/artigo?id=7" });
        var link = await ClienteDaApi.LerAsync<LinkResponse>(resposta, HttpStatusCode.Created);

        Assert.True(ValidadorDeCodigo.EhValido(link.Codigo));
        Assert.Equal(GeradorDeCodigo.TamanhoPadrao, link.Codigo.Length);
        Assert.Equal($"http://localhost/{link.Codigo}", link.UrlCurta);
        Assert.Equal(new Uri(link.UrlCurta), resposta.Headers.Location);
        Assert.Equal("https://exemplo.com/artigo?id=7", link.UrlOriginal);
        Assert.True(link.Ativo);
        Assert.Null(link.ExpiraEm);
        Assert.Equal(SituacaoDoLink.Disponivel, link.Situacao);
    }

    [Fact]
    public async Task Criar_CodigoPersonalizadoEmUsoPorOutroUsuario_Retorna409()
    {
        var codigo = ClienteDaApi.CodigoUnico();
        await (await api.CriarUsuarioAsync()).CriarLinkAsync(codigo: codigo);

        var resposta = await (await api.CriarUsuarioAsync()).PostAsJsonAsync("/api/links",
            new { urlOriginal = "https://exemplo.com", codigo });

        var problema = await ClienteDaApi.LerProblemaAsync(resposta, HttpStatusCode.Conflict);
        Assert.Equal($"O código '{codigo}' já está em uso.", problema.Detail);
    }

    [Fact]
    public async Task Criar_MesmoCodigoAoMesmoTempo_SoUmaDasRequisicoesConsegue()
    {
        var codigo = ClienteDaApi.CodigoUnico();
        var clientes = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => api.CriarUsuarioAsync()));

        var respostas = await Task.WhenAll(clientes.Select(c =>
            c.PostAsJsonAsync("/api/links", new { urlOriginal = "https://exemplo.com", codigo })));

        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(respostas.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Fact]
    public async Task Criar_DadosInvalidos_Retorna400ComOErroDeCadaCampo()
    {
        var cliente = await api.CriarUsuarioAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/links",
            new { urlOriginal = "ftp://exemplo.com", codigo = "ab", expiraEm = "2020-01-01T00:00:00Z" });

        var problema = await ClienteDaApi.LerProblemaAsync(resposta, HttpStatusCode.BadRequest);
        Assert.Equal(["codigo", "expiraEm", "urlOriginal"], problema.Errors.Keys.Order());
    }

    [Fact]
    public async Task Listar_MostraSoOsLinksDoProprioUsuario_DoMaisRecenteParaOMaisAntigo()
    {
        var ana = await api.CriarUsuarioAsync();
        var bruno = await api.CriarUsuarioAsync();
        var primeiro = await ana.CriarLinkAsync("https://exemplo.com/1");
        var segundo = await ana.CriarLinkAsync("https://exemplo.com/2");
        await bruno.CriarLinkAsync("https://exemplo.com/do-bruno");

        var links = await ClienteDaApi.LerAsync<List<LinkResponse>>(await ana.GetAsync("/api/links"), HttpStatusCode.OK);

        Assert.Equal([segundo.Codigo, primeiro.Codigo], links.Select(l => l.Codigo));
    }

    [Fact]
    public async Task Atualizar_ExpiracaoComDataENull_DefineERemoveAExpiracao()
    {
        var cliente = await api.CriarUsuarioAsync();
        var link = await cliente.CriarLinkAsync();
        var data = DateTimeOffset.UtcNow.AddDays(3).ToOffset(TimeSpan.FromHours(-3));
        var texto = data.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

        var comData = await ClienteDaApi.LerAsync<LinkResponse>(
            await cliente.AtualizarLinkAsync(link.Codigo, $$"""{"expiraEm":"{{texto}}"}"""), HttpStatusCode.OK);
        Assert.Equal(data.UtcDateTime, comData.ExpiraEm!.Value, TimeSpan.FromSeconds(1));
        Assert.Equal(DateTimeKind.Utc, comData.ExpiraEm.Value.Kind);
        Assert.True(comData.Ativo);

        // Campo ausente não muda nada; null remove a expiração.
        var semMudanca = await ClienteDaApi.LerAsync<LinkResponse>(await cliente.AtualizarLinkAsync(link.Codigo, "{}"), HttpStatusCode.OK);
        Assert.Equal(comData.ExpiraEm, semMudanca.ExpiraEm);

        var semData = await ClienteDaApi.LerAsync<LinkResponse>(
            await cliente.AtualizarLinkAsync(link.Codigo, """{"expiraEm":null}"""), HttpStatusCode.OK);
        Assert.Null(semData.ExpiraEm);
    }

    [Fact]
    public async Task Atualizar_DataNoPassado_Retorna400SemAlterarOLink()
    {
        var cliente = await api.CriarUsuarioAsync();
        var link = await cliente.CriarLinkAsync();

        var resposta = await cliente.AtualizarLinkAsync(link.Codigo, """{"ativo":false,"expiraEm":"2020-01-01T00:00:00-03:00"}""");

        var problema = await ClienteDaApi.LerProblemaAsync(resposta, HttpStatusCode.BadRequest);
        Assert.Equal(["expiraEm"], problema.Errors.Keys);
        var depois = await ClienteDaApi.LerAsync<List<LinkResponse>>(await cliente.GetAsync("/api/links"), HttpStatusCode.OK);
        Assert.True(Assert.Single(depois).Ativo);
    }

    [Fact]
    public async Task LinkDeOutroUsuario_Retorna404EmTodasAsRotasDoDono()
    {
        var link = await (await api.CriarUsuarioAsync()).CriarLinkAsync();
        var intruso = await api.CriarUsuarioAsync();

        var respostas = new[]
        {
            await intruso.AtualizarLinkAsync(link.Codigo, """{"ativo":false}"""),
            await intruso.DeleteAsync($"/api/links/{link.Codigo}"),
            await intruso.GetAsync($"/api/links/{link.Codigo}/qrcode"),
            await intruso.GetAsync($"/api/links/{link.Codigo}/estatisticas"),
        };

        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        Assert.Equal(HttpStatusCode.Found, (await api.CriarCliente().GetAsync($"/{link.Codigo}")).StatusCode);
    }

    [Fact]
    public async Task Excluir_ApagaOLinkEOsCliquesDele()
    {
        var cliente = await api.CriarUsuarioAsync();
        var link = await cliente.CriarLinkAsync();
        await api.CriarCliente().GetAsync($"/{link.Codigo}");
        Assert.Equal(1, (await cliente.AguardarCliquesAsync(link.Codigo, total: 1)).TotalDeCliques);

        await using var db = api.CriarDbContext();
        var id = await db.Links.Where(l => l.Codigo == link.Codigo).Select(l => l.Id).SingleAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/links/{link.Codigo}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await api.CriarCliente().GetAsync($"/{link.Codigo}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.DeleteAsync($"/api/links/{link.Codigo}")).StatusCode);
        Assert.False(await db.Clicks.AnyAsync(c => c.LinkId == id));
    }

    [Fact]
    public async Task QrCode_DevolveUmPngQueONavegadorPodeGuardar()
    {
        var cliente = await api.CriarUsuarioAsync();
        var link = await cliente.CriarLinkAsync();

        var resposta = await cliente.GetAsync($"/api/links/{link.Codigo}/qrcode?tamanho=4");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("image/png", resposta.Content.Headers.ContentType?.MediaType);
        Assert.True(resposta.Headers.CacheControl?.Private);
        Assert.Equal(TimeSpan.FromDays(1), resposta.Headers.CacheControl?.MaxAge);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, (await resposta.Content.ReadAsByteArrayAsync())[..4]);

        var tamanhoInvalido = await cliente.GetAsync($"/api/links/{link.Codigo}/qrcode?tamanho=1");
        Assert.Equal(HttpStatusCode.BadRequest, tamanhoInvalido.StatusCode);
    }
}
