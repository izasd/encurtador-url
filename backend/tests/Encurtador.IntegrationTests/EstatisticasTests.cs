using System.Net;
using Encurtador.Api.Cliques;
using Encurtador.IntegrationTests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.IntegrationTests;

[Collection(ColecaoDaApi.Nome)]
public class EstatisticasTests(ApiFactory api)
{
    [Fact]
    public async Task Estatisticas_SeparamOsDiasNoFusoHorarioPedido()
    {
        var dono = await api.CriarUsuarioAsync();
        var link = await dono.CriarLinkAsync();
        await InserirCliquesAsync(link.Codigo,
            Clique(new DateTime(2026, 3, 1, 2, 30, 0, DateTimeKind.Utc)), // 28/02, 23h30 em Brasília (UTC-3)
            Clique(new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc))); // 01/03, 9h em Brasília

        var emUtc = await dono.ObterEstatisticasAsync(link.Codigo);
        var emBrasilia = await dono.ObterEstatisticasAsync(link.Codigo, "America/Sao_Paulo");

        Assert.Equal(new[] { new CliquesPorDia(new DateOnly(2026, 3, 1), 2) }, emUtc.PorDia);
        Assert.Equal(
            new[] { new CliquesPorDia(new DateOnly(2026, 2, 28), 1), new CliquesPorDia(new DateOnly(2026, 3, 1), 1) },
            emBrasilia.PorDia);
        Assert.Equal("America/Sao_Paulo", emBrasilia.FusoHorario);
    }

    [Fact]
    public async Task Estatisticas_ContamPorCategoria_ComNomeParaOQueNaoFoiIdentificado()
    {
        var dono = await api.CriarUsuarioAsync();
        var link = await dono.CriarLinkAsync();
        var agora = DateTime.UtcNow;
        await InserirCliquesAsync(link.Codigo,
            Clique(agora, pais: "BR", navegador: "Chrome", dispositivo: "Computador", referer: "t.co"),
            Clique(agora, pais: null, navegador: "Firefox", dispositivo: "Celular", referer: null),
            Clique(agora, pais: "BR", navegador: "Chrome", dispositivo: "Celular", referer: null));

        var estatisticas = await dono.ObterEstatisticasAsync(link.Codigo);

        Assert.Equal(3, estatisticas.TotalDeCliques);
        Assert.Equal(new[] { new CliquesPorCategoria("BR", 2), new CliquesPorCategoria(AnalisadorDeAcesso.Desconhecido, 1) }, estatisticas.PorPais);
        Assert.Equal(new[] { new CliquesPorCategoria("Chrome", 2), new CliquesPorCategoria("Firefox", 1) }, estatisticas.PorNavegador);
        Assert.Equal(new[] { new CliquesPorCategoria("Celular", 2), new CliquesPorCategoria("Computador", 1) }, estatisticas.PorDispositivo);
        Assert.Equal(new[] { new CliquesPorCategoria("Direto", 2), new CliquesPorCategoria("t.co", 1) }, estatisticas.PorReferer);
    }

    [Fact]
    public async Task Estatisticas_FusoHorarioInexistente_Retorna400()
    {
        var dono = await api.CriarUsuarioAsync();
        var link = await dono.CriarLinkAsync();

        var resposta = await dono.GetAsync($"/api/links/{link.Codigo}/estatisticas?fusoHorario=Lugar/Nenhum");

        var problema = await ClienteDaApi.LerProblemaAsync(resposta, HttpStatusCode.BadRequest);
        Assert.Equal(["fusoHorario"], problema.Errors.Keys);
    }

    private static Click Clique(
        DateTime criadoEm, string? pais = null, string navegador = "Chrome", string dispositivo = "Computador", string? referer = null) =>
        new() { CriadoEm = criadoEm, Pais = pais, Navegador = navegador, Dispositivo = dispositivo, Referer = referer };

    private async Task InserirCliquesAsync(string codigo, params Click[] cliques)
    {
        await using var db = api.CriarDbContext();
        var linkId = await db.Links.Where(l => l.Codigo == codigo).Select(l => l.Id).SingleAsync();

        foreach (var clique in cliques)
        {
            clique.LinkId = linkId;
        }

        db.Clicks.AddRange(cliques);
        await db.SaveChangesAsync();
    }
}
