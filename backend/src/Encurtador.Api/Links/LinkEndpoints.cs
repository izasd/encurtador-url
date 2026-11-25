using Encurtador.Api.Dados;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Links;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var links = app.MapGroup("/api/links").WithTags("Links");

        links.MapPost("/", CriarLink)
            .WithSummary("Cria um link curto");

        links.MapGet("/", ListarLinks)
            .WithSummary("Lista os links cadastrados, do mais recente para o mais antigo");

        // Só aceita códigos de 4 a 10 letras/números (como na v1), para a rota
        // não capturar caminhos como /favicon.ico. As chaves da regex são
        // duplicadas porque { } delimitam parâmetros no template da rota.
        app.MapGet("/{codigo:regex(^[a-zA-Z0-9]{{4,10}}$)}", Redirecionar)
            .WithTags("Redirecionamento")
            .WithSummary("Redireciona para a URL original");

        return app;
    }

    private static async Task<Results<Created<LinkResponse>, BadRequest<RespostaErro>>> CriarLink(
        CriarLinkRequest requisicao, AppDbContext db, HttpRequest http, CancellationToken ct)
    {
        if (!ValidadorDeUrl.EhValida(requisicao.UrlOriginal))
        {
            return TypedResults.BadRequest(new RespostaErro("URL inválida. Forneça http:// ou https://"));
        }

        var link = new Link
        {
            Codigo = GeradorDeCodigo.Gerar(),
            UrlOriginal = requisicao.UrlOriginal,
            CriadoEm = DateTime.UtcNow,
        };

        db.Links.Add(link);
        await db.SaveChangesAsync(ct);

        var resposta = ParaResposta(link, http);
        return TypedResults.Created(resposta.UrlCurta, resposta);
    }

    private static async Task<Ok<List<LinkResponse>>> ListarLinks(
        AppDbContext db, HttpRequest http, CancellationToken ct)
    {
        var links = await db.Links
            .AsNoTracking()
            .OrderByDescending(l => l.Id)
            .ToListAsync(ct);

        return TypedResults.Ok(links.Select(l => ParaResposta(l, http)).ToList());
    }

    private static async Task<Results<RedirectHttpResult, NotFound<RespostaErro>>> Redirecionar(
        string codigo, AppDbContext db, CancellationToken ct)
    {
        var urlOriginal = await db.Links
            .Where(l => l.Codigo == codigo)
            .Select(l => l.UrlOriginal)
            .FirstOrDefaultAsync(ct);

        if (urlOriginal is null)
        {
            return TypedResults.NotFound(new RespostaErro("Código não encontrado"));
        }

        return TypedResults.Redirect(urlOriginal);
    }

    private static LinkResponse ParaResposta(Link link, HttpRequest http) =>
        new(link.Codigo, link.UrlOriginal, $"{http.Scheme}://{http.Host}{http.PathBase}/{link.Codigo}", link.CriadoEm);
}
