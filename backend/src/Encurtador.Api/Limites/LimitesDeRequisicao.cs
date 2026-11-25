using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Encurtador.Api.Limites;

/// <summary>Limites de requisições por janela de tempo, para conter abuso e tentativas de adivinhar senhas.</summary>
public static class LimitesDeRequisicao
{
    public const string CriacaoDeLinks = "criacao-de-links";
    public const string Autenticacao = "autenticacao";
    public const string Visitante = "visitante";

    public static IServiceCollection AddLimitesDeRequisicao(this IServiceCollection services)
    {
        services.AddRateLimiter(opcoes =>
        {
            // O padrão seria 503 sem corpo; 429 é o código certo para "muitas requisições".
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opcoes.OnRejected = ResponderLimiteAtingidoAsync;

            // Criação de links: 30 por minuto para cada usuário.
            opcoes.AddPolicy(CriacaoDeLinks, http => JanelaFixa(
                http.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? EnderecoIp(http), limite: 30, TimeSpan.FromMinutes(1)));

            // Login e cadastro: 10 por minuto por endereço IP, contra tentativas de adivinhar senhas.
            opcoes.AddPolicy(Autenticacao, http => JanelaFixa(EnderecoIp(http), limite: 10, TimeSpan.FromMinutes(1)));

            // Contas de visitante: 10 por hora por endereço IP (cada uma cria centenas de linhas no banco).
            opcoes.AddPolicy(Visitante, http => JanelaFixa(EnderecoIp(http), limite: 10, TimeSpan.FromHours(1)));
        });

        return services;
    }

    /// <summary>Texto amigável para o tempo de espera ("40 segundos", "58 minutos").</summary>
    public static string DescreverEspera(TimeSpan espera)
    {
        var segundos = (int)Math.Ceiling(espera.TotalSeconds);
        if (segundos < 60)
        {
            return segundos == 1 ? "1 segundo" : $"{segundos} segundos";
        }

        var minutos = (int)Math.Ceiling(espera.TotalMinutes);
        return minutos == 1 ? "1 minuto" : $"{minutos} minutos";
    }

    private static RateLimitPartition<string> JanelaFixa(string chave, int limite, TimeSpan janela) =>
        RateLimitPartition.GetFixedWindowLimiter(chave, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limite,
            Window = janela,
            QueueLimit = 0,
        });

    // Atrás de um proxy reverso, só é o IP do visitante com a seção "ProxyReverso" ligada
    // (ver Hospedagem/ProxyReverso.cs); senão seria o IP do proxy, igual para todo mundo.
    private static string EnderecoIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

    private static async ValueTask ResponderLimiteAtingidoAsync(OnRejectedContext contexto, CancellationToken ct)
    {
        var http = contexto.HttpContext;
        var detalhe = "Muitas requisições em pouco tempo. Tente de novo daqui a pouco.";

        if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
        {
            http.Response.Headers.RetryAfter = Math.Ceiling(espera.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            detalhe = $"Muitas requisições em pouco tempo. Tente de novo em {DescreverEspera(espera)}.";
        }

        await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = { Status = StatusCodes.Status429TooManyRequests, Detail = detalhe },
        });
    }
}
