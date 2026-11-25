using Encurtador.Api.Cliques;
using Encurtador.Api.Dados;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Links;

public static class LinkEndpoints
{
    private const int MaximoDeTentativasDeCodigo = 5;

    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var links = app.MapGroup("/api/links").WithTags("Links");

        links.MapPost("/", CriarLink)
            .WithSummary("Cria um link curto, com código gerado ou personalizado")
            .ProducesProblem(StatusCodes.Status409Conflict);

        links.MapGet("/", ListarLinks)
            .WithSummary("Lista os links cadastrados, do mais recente para o mais antigo");

        app.MapGet("/{codigo:codigoCurto}", Redirecionar)
            .WithTags("Redirecionamento")
            .WithSummary("Redireciona para a URL original")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Created<LinkResponse>, ValidationProblem, ProblemHttpResult>> CriarLink(
        CriarLinkRequest requisicao, AppDbContext db, HttpRequest http, CancellationToken ct)
    {
        var erros = new Dictionary<string, string[]>();

        if (!ValidadorDeUrl.TentarNormalizar(requisicao.UrlOriginal, out var urlOriginal))
        {
            erros["urlOriginal"] = [$"Informe uma URL válida, começando com http:// ou https:// e com até {Link.TamanhoMaximoUrl} caracteres."];
        }

        // Código em branco conta como "não informado": nesse caso a API gera um.
        var codigoPersonalizado = string.IsNullOrWhiteSpace(requisicao.Codigo) ? null : requisicao.Codigo.Trim();
        if (codigoPersonalizado is not null && !ValidadorDeCodigo.EhValido(codigoPersonalizado))
        {
            erros["codigo"] = [$"O código deve ter de {Link.TamanhoMinimoCodigo} a {Link.TamanhoMaximoCodigo} caracteres, apenas letras e números."];
        }

        if (erros.Count > 0)
        {
            return TypedResults.ValidationProblem(erros);
        }

        var criadoEm = DateTime.UtcNow;

        if (codigoPersonalizado is not null)
        {
            var link = new Link { Codigo = codigoPersonalizado, UrlOriginal = urlOriginal, CriadoEm = criadoEm };

            if (await TentarSalvarAsync(db, link, ct))
            {
                return Criado(link, http);
            }

            return TypedResults.Problem(
                detail: $"O código '{codigoPersonalizado}' já está em uso.",
                statusCode: StatusCodes.Status409Conflict);
        }

        // Com 62^6 combinações uma colisão é raríssima, mas se o código gerado
        // já existir, outro é gerado no lugar.
        for (var tentativa = 1; tentativa <= MaximoDeTentativasDeCodigo; tentativa++)
        {
            var link = new Link { Codigo = GeradorDeCodigo.Gerar(), UrlOriginal = urlOriginal, CriadoEm = criadoEm };

            if (await TentarSalvarAsync(db, link, ct))
            {
                return Criado(link, http);
            }
        }

        throw new InvalidOperationException(
            $"Não foi possível gerar um código livre em {MaximoDeTentativasDeCodigo} tentativas.");
    }

    /// <summary>
    /// Salva o link se o código estiver livre. Retorna false se o código já estiver em uso.
    /// </summary>
    private static async Task<bool> TentarSalvarAsync(AppDbContext db, Link link, CancellationToken ct)
    {
        // Verifica antes de salvar: é o caminho comum e não depende de exceção.
        if (await db.Links.AnyAsync(l => l.Codigo == link.Codigo, ct))
        {
            return false;
        }

        db.Links.Add(link);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.EhViolacaoDeUnicidade())
        {
            // Outra requisição salvou o mesmo código entre a verificação e o INSERT.
            // O índice único garante que só uma vence; esta tira o link do contexto
            // para que ele não seja reenviado no próximo SaveChanges.
            db.Entry(link).State = EntityState.Detached;
            return false;
        }
    }

    private static Created<LinkResponse> Criado(Link link, HttpRequest http)
    {
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
        string codigo, AppDbContext db, FilaDeCliques fila, HttpRequest http, CancellationToken ct)
    {
        var link = await db.Links
            .Where(l => l.Codigo == codigo)
            .Select(l => new { l.Id, l.UrlOriginal })
            .FirstOrDefaultAsync(ct);

        if (link is null)
        {
            return TypedResults.Problem(
                detail: $"Nenhum link encontrado com o código '{codigo}'.",
                statusCode: StatusCodes.Status404NotFound);
        }

        // Só captura os dados brutos: a análise e a gravação do clique acontecem em
        // segundo plano, para o redirecionamento não esperar o banco.
        fila.TentarEnfileirar(new CliqueRegistrado(
            link.Id,
            DateTime.UtcNow,
            http.Headers.UserAgent.ToString(),
            http.Headers.Referer.ToString(),
            http.Headers["CF-IPCountry"].ToString()));

        return TypedResults.Redirect(link.UrlOriginal);
    }

    private static LinkResponse ParaResposta(Link link, HttpRequest http) =>
        new(link.Codigo, link.UrlOriginal, $"{http.Scheme}://{http.Host}{http.PathBase}/{link.Codigo}", link.CriadoEm);
}
