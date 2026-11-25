using System.Diagnostics;
using System.Security.Cryptography;
using Encurtador.Api.Cliques;
using Encurtador.Api.Dados;
using Encurtador.Api.Links;
using Microsoft.AspNetCore.Identity;

namespace Encurtador.Api.Usuarios;

/// <summary>
/// Cria contas de visitante com links e cliques de exemplo, para quem quer conhecer o
/// encurtador sem se cadastrar. As contas são apagadas pela <see cref="LimpezaDeVisitantes"/>.
/// </summary>
public static class DadosDeExemplo
{
    /// <summary>Tempo de vida de uma conta de visitante; maior que a validade do token (8 horas).</summary>
    public static readonly TimeSpan Validade = TimeSpan.FromHours(24);

    private static readonly (string Url, int DiasAtras, int Cliques)[] Links =
    [
        ("https://github.com/", 28, 140),
        ("https://learn.microsoft.com/dotnet/", 21, 95),
        ("https://angular.dev/", 12, 70),
        ("https://www.postgresql.org/docs/", 5, 35),
    ];

    private static readonly (string Valor, int Peso)[] Navegadores =
        [("Chrome", 55), ("Safari", 21), ("Edge", 9), ("Firefox", 7), ("Samsung Internet", 5), ("Opera", 3)];

    private static readonly (string Valor, int Peso)[] Dispositivos =
        [("Celular", 58), ("Computador", 36), ("Tablet", 6)];

    private static readonly (string? Valor, int Peso)[] Paises =
        [("BR", 70), ("PT", 9), ("US", 7), ("AR", 4), (null, 10)];

    private static readonly (string? Valor, int Peso)[] Referers =
        [(null, 28), ("www.instagram.com", 20), ("t.co", 15), ("www.linkedin.com", 14), ("www.google.com", 13), ("l.facebook.com", 10)];

    public static async Task<User> CriarVisitanteAsync(
        AppDbContext db, IPasswordHasher<User> hasher, DateTime agora, CancellationToken ct)
    {
        var visitante = new User
        {
            Nome = "Visitante",
            Email = $"visitante-{Guid.NewGuid():N}@visitante.invalid",
            SenhaHash = string.Empty,
            CriadoEm = agora,
            EhVisitante = true,
        };

        // Senha aleatória que ninguém conhece: a conta só é usada pelo token devolvido agora.
        visitante.SenhaHash = hasher.HashPassword(visitante, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

        // Tudo ou nada: se algo falhar, nenhuma parte da conta de exemplo fica no banco.
        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        db.Users.Add(visitante);
        await db.SaveChangesAsync(ct);

        var links = Links
            .Select(l => new Link
            {
                UserId = visitante.Id,
                Codigo = GeradorDeCodigo.Gerar(),
                UrlOriginal = l.Url,
                CriadoEm = agora.AddDays(-l.DiasAtras),
            })
            .ToList();

        db.Links.AddRange(links);
        await db.SaveChangesAsync(ct);

        for (var i = 0; i < links.Count; i++)
        {
            db.Clicks.AddRange(GerarCliques(links[i].Id, links[i].CriadoEm, agora, Links[i].Cliques, Random.Shared));
        }

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        return visitante;
    }

    /// <summary>
    /// Gera cliques fictícios entre a criação do link e agora: a maioria logo depois da criação,
    /// como acontece quando um link é divulgado, e cada vez menos com o passar dos dias.
    /// </summary>
    public static List<Click> GerarCliques(int linkId, DateTime desde, DateTime ate, int quantidade, Random aleatorio)
    {
        var janela = ate - desde;
        var cliques = new List<Click>(quantidade);

        for (var i = 0; i < quantidade; i++)
        {
            // Distribuição exponencial com média em 1/4 do período; o que passaria de "agora"
            // é sorteado de novo de forma uniforme.
            var fracao = -Math.Log(1 - aleatorio.NextDouble()) / 4;
            if (fracao >= 1)
            {
                fracao = aleatorio.NextDouble();
            }

            var robo = aleatorio.Next(100) < 4;

            cliques.Add(new Click
            {
                LinkId = linkId,
                CriadoEm = desde + janela * fracao,
                Navegador = robo ? AnalisadorDeAcesso.Robo : Sortear(Navegadores, aleatorio),
                Dispositivo = robo ? AnalisadorDeAcesso.Robo : Sortear(Dispositivos, aleatorio),
                Pais = Sortear(Paises, aleatorio),
                Referer = robo ? null : Sortear(Referers, aleatorio),
            });
        }

        return cliques;
    }

    private static T Sortear<T>((T Valor, int Peso)[] opcoes, Random aleatorio)
    {
        var alvo = aleatorio.Next(opcoes.Sum(o => o.Peso));

        foreach (var (valor, peso) in opcoes)
        {
            if (alvo < peso)
            {
                return valor;
            }

            alvo -= peso;
        }

        throw new UnreachableException();
    }
}
