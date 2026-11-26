using Encurtador.Api.Usuarios;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Encurtador.Tests.Usuarios;

public class GeradorDeTokenTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    private static readonly OpcoesJwt Opcoes = new()
    {
        Chave = "chave-de-teste-com-pelo-menos-32-caracteres",
        Emissor = "emissor-de-teste",
        Publico = "publico-de-teste",
        ExpiracaoEmMinutos = 60,
    };

    private static readonly User Usuario = new() { Id = 42, Nome = "Ana Souza", Email = "ana@exemplo.com", SenhaHash = "x" };

    [Fact]
    public async Task Gerar_CriaTokenAssinadoComOsDadosDoUsuario()
    {
        var gerado = new GeradorDeToken(Options.Create(Opcoes), new RelogioFixo(Agora)).Gerar(Usuario);

        var resultado = await ValidarAsync(gerado.Token, Opcoes.ChaveDeAssinatura());

        Assert.True(resultado.IsValid, resultado.Exception?.Message);
        Assert.Equal("42", resultado.Claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal("ana@exemplo.com", resultado.Claims[JwtRegisteredClaimNames.Email]);
        Assert.Equal("Ana Souza", resultado.Claims[JwtRegisteredClaimNames.Name]);
    }

    [Fact]
    public async Task Gerar_ExpiraDepoisDoTempoConfigurado()
    {
        var gerado = new GeradorDeToken(Options.Create(Opcoes), new RelogioFixo(Agora)).Gerar(Usuario);

        var token = (JsonWebToken)(await ValidarAsync(gerado.Token, Opcoes.ChaveDeAssinatura())).SecurityToken;

        Assert.Equal(Agora.AddMinutes(60), gerado.ExpiraEm);
        Assert.Equal(Agora.UtcDateTime, token.IssuedAt);
        Assert.Equal(gerado.ExpiraEm.UtcDateTime, token.ValidTo);
    }

    [Fact]
    public async Task Gerar_TokenNaoPassaNaValidacaoComOutraChave()
    {
        var gerado = new GeradorDeToken(Options.Create(Opcoes), new RelogioFixo(Agora)).Gerar(Usuario);
        var outraChave = new SymmetricSecurityKey("outra-chave-com-pelo-menos-32-caracteres"u8.ToArray());

        var resultado = await ValidarAsync(gerado.Token, outraChave);

        Assert.False(resultado.IsValid);
    }

    private static Task<TokenValidationResult> ValidarAsync(string token, SecurityKey chave) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Opcoes.Emissor,
            ValidAudience = Opcoes.Publico,
            IssuerSigningKey = chave,
            // O relógio do teste está fixo no passado; os horários são conferidos à parte.
            ValidateLifetime = false,
        });

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}
