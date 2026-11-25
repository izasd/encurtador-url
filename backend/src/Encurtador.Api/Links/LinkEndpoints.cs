using System.Security.Claims;
using Encurtador.Api.Cliques;
using Encurtador.Api.Dados;
using Encurtador.Api.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Links;

public static class LinkEndpoints
{
    private const int MaximoDeTentativasDeCodigo = 5;

    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        // Criar e listar links exige login; o redirecionamento continua público.
        var links = app.MapGroup("/api/links").WithTags("Links").RequireAuthorization();

        links.MapPost("/", CriarLink)
            .WithSummary("Cria um link curto, com código gerado ou personalizado e expiração opcional")
            .ProducesProblem(StatusCodes.Status409Conflict);

        links.MapGet("/", ListarLinks)
            .WithSummary("Lista os links do usuário, do mais recente para o mais antigo");

        app.MapGet("/{codigo:codigoCurto}", Redirecionar)
            .WithTags("Redirecionamento")
            .WithSummary("Redireciona para a URL original")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone);

        return app;
    }

    private static async Task<Results<Created<LinkResponse>, ValidationProblem, ProblemHttpResult>> CriarLink(
        CriarLinkRequest requisicao, AppDbContext db, HttpRequest http, ClaimsPrincipal usuario, TimeProvider relogio,
        CancellationToken ct)
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
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

        DateTime? expiraEm = null;
        if (requisicao.ExpiraEm is not null)
        {
            if (ValidadorDeExpiracao.TentarLer(requisicao.ExpiraEm, agora, out var data))
            {
                expiraEm = data;
            }
            else
            {
                erros["expiraEm"] = [ValidadorDeExpiracao.Mensagem];
            }
        }

        if (erros.Count > 0)
        {
            return TypedResults.ValidationProblem(erros);
        }

        var userId = usuario.ObterIdDoUsuario();
        Link NovoLink(string codigo) =>
            new() { UserId = userId, Codigo = codigo, UrlOriginal = urlOriginal, CriadoEm = agora, ExpiraEm = expiraEm };

        if (codigoPersonalizado is not null)
        {
            var link = NovoLink(codigoPersonalizado);

            if (await TentarSalvarAsync(db, link, ct))
            {
                return Criado(link, http, agora);
            }

            return TypedResults.Problem(
                detail: $"O código '{codigoPersonalizado}' já está em uso.",
                statusCode: StatusCodes.Status409Conflict);
        }

        // Com 62^6 combinações uma colisão é raríssima, mas se o código gerado
        // já existir, outro é gerado no lugar.
        for (var tentativa = 1; tentativa <= MaximoDeTentativasDeCodigo; tentativa++)
        {
            var link = NovoLink(GeradorDeCodigo.Gerar());

            if (await TentarSalvarAsync(db, link, ct))
            {
                return Criado(link, http, agora);
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

    private static Created<LinkResponse> Criado(Link link, HttpRequest http, DateTime agora)
    {
        var resposta = ParaResposta(link, http, agora);
        return TypedResults.Created(resposta.UrlCurta, resposta);
    }

    private static async Task<Ok<List<LinkResponse>>> ListarLinks(
        AppDbContext db, HttpRequest http, ClaimsPrincipal usuario, TimeProvider relogio, CancellationToken ct)
    {
        var userId = usuario.ObterIdDoUsuario();
        var agora = relogio.GetUtcNow().UtcDateTime;

        var links = await db.Links
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Id)
            .ToListAsync(ct);

        return TypedResults.Ok(links.Select(l => ParaResposta(l, http, agora)).ToList());
    }

    private static async Task<Results<RedirectHttpResult, ProblemHttpResult>> Redirecionar(
        string codigo, AppDbContext db, FilaDeCliques fila, HttpRequest http, TimeProvider relogio, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
        var link = await db.Links
            .Where(l => l.Codigo == codigo)
            .Select(l => new { l.Id, l.UrlOriginal, l.Ativo, l.ExpiraEm })
            .FirstOrDefaultAsync(ct);

        if (link is null)
        {
            return TypedResults.Problem(
                detail: $"Nenhum link encontrado com o código '{codigo}'.",
                statusCode: StatusCodes.Status404NotFound);
        }

        // Desativado ou expirado: 410 (o link existe, mas foi tirado do ar de propósito).
        // O clique não é registrado.
        switch (Link.Situacao(link.Ativo, link.ExpiraEm, agora))
        {
            case SituacaoDoLink.Desativado:
                return TypedResults.Problem(detail: "Este link foi desativado pelo dono.", statusCode: StatusCodes.Status410Gone);
            case SituacaoDoLink.Expirado:
                return TypedResults.Problem(detail: "Este link expirou.", statusCode: StatusCodes.Status410Gone);
        }

        // Só captura os dados brutos: a análise e a gravação do clique acontecem em
        // segundo plano, para o redirecionamento não esperar o banco.
        fila.TentarEnfileirar(new CliqueRegistrado(
            link.Id,
            agora,
            http.Headers.UserAgent.ToString(),
            http.Headers.Referer.ToString(),
            http.Headers["CF-IPCountry"].ToString()));

        return TypedResults.Redirect(link.UrlOriginal);
    }

    private static LinkResponse ParaResposta(Link link, HttpRequest http, DateTime agora) =>
        new(
            link.Codigo,
            link.UrlOriginal,
            UrlCurta.Montar(http, link.Codigo),
            link.CriadoEm,
            link.Ativo,
            link.ExpiraEm,
            Link.Situacao(link.Ativo, link.ExpiraEm, agora));
}
