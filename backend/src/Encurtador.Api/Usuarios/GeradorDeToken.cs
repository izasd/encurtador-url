using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Encurtador.Api.Usuarios;

public record TokenGerado(string Token, DateTimeOffset ExpiraEm);

public class GeradorDeToken(IOptions<OpcoesJwt> opcoes, TimeProvider relogio)
{
    private readonly JsonWebTokenHandler _handler = new();

    public TokenGerado Gerar(User usuario)
    {
        var jwt = opcoes.Value;

        // O JWT guarda os horários em segundos; arredondar aqui mantém ExpiraEm igual ao "exp" do token.
        var agora = DateTimeOffset.FromUnixTimeSeconds(relogio.GetUtcNow().ToUnixTimeSeconds());
        var expiraEm = agora.AddMinutes(jwt.ExpiracaoEmMinutos);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Emissor,
            Audience = jwt.Publico,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),
            ]),
            SigningCredentials = new SigningCredentials(jwt.ChaveDeAssinatura(), SecurityAlgorithms.HmacSha256),
        });

        return new TokenGerado(token, expiraEm);
    }
}
