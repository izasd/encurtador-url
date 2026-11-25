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
            .WithSummary("Redireciona para a URL original")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Created<LinkResponse>, ValidationProblem>> CriarLink(
        CriarLinkRequest requisicao, AppDbContext db, HttpRequest http, CancellationToken ct)
    {
        if (!ValidadorDeUrl.EhValida(requisicao.UrlOriginal))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["urlOriginal"] = [$"Informe uma URL válida, começando com http:// ou https:// e com até {Link.TamanhoMaximoUrl} caracteres."],
            });
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

    private static async Task<Results<RedirectHttpResult, ProblemHttpResult>> Redirecionar(
        string codigo, AppDbContext db, CancellationToken ct)
    {
        var urlOriginal = await db.Links
            .Where(l => l.Codigo == codigo)
            .Select(l => l.UrlOriginal)
            .FirstOrDefaultAsync(ct);

        if (urlOriginal is null)
        {
            return TypedResults.Problem(
                detail: $"Nenhum link encontrado com o código '{codigo}'.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Redirect(urlOriginal);
    }

    private static LinkResponse ParaResposta(Link link, HttpRequest http) =>
        new(link.Codigo, link.UrlOriginal, $"{http.Scheme}://{http.Host}{http.PathBase}/{link.Codigo}", link.CriadoEm);
}
