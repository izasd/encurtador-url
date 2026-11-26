using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Encurtador.Api.Usuarios;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id do usuário autenticado, lido da claim "sub" do JWT.</summary>
    public static int ObterIdDoUsuario(this ClaimsPrincipal usuario)
    {
        var sub = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("O usuário autenticado não tem a claim 'sub'.");

        return int.Parse(sub, CultureInfo.InvariantCulture);
    }
}
