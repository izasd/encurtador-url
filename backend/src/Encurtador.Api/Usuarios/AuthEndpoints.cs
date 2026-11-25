using Encurtador.Api.Dados;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Usuarios;

public static class AuthEndpoints
{
    // Usados no login de e-mails sem conta (ver Login). Os parâmetros do hash são os mesmos
    // do PasswordHasher registrado no Program.cs, que usa as opções padrão.
    private static readonly User UsuarioFicticio = new() { Nome = "", Email = "", SenhaHash = "" };
    private static readonly string HashFicticio =
        new PasswordHasher<User>().HashPassword(UsuarioFicticio, "senha-ficticia-para-igualar-o-tempo");

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Autenticação");

        auth.MapPost("/registrar", Registrar)
            .WithSummary("Cria uma conta")
            .ProducesProblem(StatusCodes.Status409Conflict);

        auth.MapPost("/login", Login)
            .WithSummary("Confere e-mail e senha e devolve um token JWT")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/visitante", EntrarComoVisitante)
            .WithSummary("Cria uma conta temporária de visitante, com dados de exemplo, e devolve um token JWT");

        return app;
    }

    private static async Task<Ok<LoginResponse>> EntrarComoVisitante(
        AppDbContext db, IPasswordHasher<User> hasher, GeradorDeToken tokens, TimeProvider relogio, CancellationToken ct)
    {
        var visitante = await DadosDeExemplo.CriarVisitanteAsync(db, hasher, relogio.GetUtcNow().UtcDateTime, ct);

        var token = tokens.Gerar(visitante);
        return TypedResults.Ok(new LoginResponse(token.Token, token.ExpiraEm, UsuarioResponse.De(visitante)));
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Login(
        LoginRequest requisicao, AppDbContext db, IPasswordHasher<User> hasher, GeradorDeToken tokens, CancellationToken ct)
    {
        var email = RegrasDeConta.NormalizarEmail(requisicao.Email);
        var senha = requisicao.Senha ?? string.Empty;

        var usuario = email is null ? null : await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (usuario is null)
        {
            // Confere a senha contra um hash fictício para a resposta levar o mesmo tempo de quando
            // o e-mail existe; senão, a demora revelaria quais e-mails têm conta.
            hasher.VerifyHashedPassword(UsuarioFicticio, HashFicticio, senha);
            return CredenciaisInvalidas();
        }

        var resultado = hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
        if (resultado == PasswordVerificationResult.Failed)
        {
            return CredenciaisInvalidas();
        }

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // O hash foi gerado com parâmetros antigos: aproveita a senha correta para atualizá-lo.
            usuario.SenhaHash = hasher.HashPassword(usuario, senha);
            await db.SaveChangesAsync(ct);
        }

        var token = tokens.Gerar(usuario);
        return TypedResults.Ok(new LoginResponse(token.Token, token.ExpiraEm, UsuarioResponse.De(usuario)));
    }

    // A mesma mensagem para e-mail inexistente e senha errada, para não revelar quem tem conta.
    private static ProblemHttpResult CredenciaisInvalidas() =>
        TypedResults.Problem(
            detail: "E-mail ou senha incorretos.",
            statusCode: StatusCodes.Status401Unauthorized);

    private static async Task<Results<Created<UsuarioResponse>, ValidationProblem, ProblemHttpResult>> Registrar(
        RegistrarRequest requisicao, AppDbContext db, IPasswordHasher<User> hasher, CancellationToken ct)
    {
        var nome = RegrasDeConta.NormalizarNome(requisicao.Nome);
        var email = RegrasDeConta.NormalizarEmail(requisicao.Email);
        var senha = requisicao.Senha;

        if (nome is null || email is null || !RegrasDeConta.SenhaValida(senha))
        {
            var erros = new Dictionary<string, string[]>();

            if (nome is null)
            {
                erros["nome"] = [$"Informe um nome com até {User.TamanhoMaximoNome} caracteres."];
            }

            if (email is null)
            {
                erros["email"] = ["Informe um e-mail válido."];
            }

            if (!RegrasDeConta.SenhaValida(senha))
            {
                erros["senha"] = [$"A senha deve ter de {RegrasDeConta.TamanhoMinimoSenha} a {RegrasDeConta.TamanhoMaximoSenha} caracteres."];
            }

            return TypedResults.ValidationProblem(erros);
        }

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return EmailEmUso();
        }

        var usuario = new User { Nome = nome, Email = email, SenhaHash = string.Empty, CriadoEm = DateTime.UtcNow };
        usuario.SenhaHash = hasher.HashPassword(usuario, senha);

        db.Users.Add(usuario);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.EhViolacaoDeUnicidade())
        {
            // Outra requisição cadastrou o mesmo e-mail entre a verificação e o INSERT.
            return EmailEmUso();
        }

        return TypedResults.Created((string?)null, UsuarioResponse.De(usuario));
    }

    private static ProblemHttpResult EmailEmUso() =>
        TypedResults.Problem(
            detail: "Já existe uma conta com este e-mail.",
            statusCode: StatusCodes.Status409Conflict);
}
