using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Encurtador.Api.Usuarios;

public static class ConfiguracaoDeAutenticacao
{
    public static IServiceCollection AddAutenticacaoJwt(this IServiceCollection services, IConfiguration configuracao)
    {
        // Falha logo na inicialização se a chave estiver ausente ou curta demais.
        services.AddOptions<OpcoesJwt>()
            .Bind(configuracao.GetSection(OpcoesJwt.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<OpcoesJwt>>((bearer, opcoes) =>
            {
                var jwt = opcoes.Value;

                // Mantém os nomes padrão do JWT ("sub", "email", "name") em vez de convertê-los
                // para os tipos longos do .NET (http://schemas.xmlsoap.org/...).
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Emissor,
                    ValidAudience = jwt.Publico,
                    IssuerSigningKey = jwt.ChaveDeAssinatura(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();
        services.AddSingleton<GeradorDeToken>();

        return services;
    }
}
