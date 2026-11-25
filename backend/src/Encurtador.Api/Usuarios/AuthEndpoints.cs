using Encurtador.Api.Dados;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Usuarios;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Autenticação");

        auth.MapPost("/registrar", Registrar)
            .WithSummary("Cria uma conta")
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

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

        return TypedResults.Created((string?)null, new UsuarioResponse(usuario.Nome, usuario.Email));
    }

    private static ProblemHttpResult EmailEmUso() =>
        TypedResults.Problem(
            detail: "Já existe uma conta com este e-mail.",
            statusCode: StatusCodes.Status409Conflict);
}
