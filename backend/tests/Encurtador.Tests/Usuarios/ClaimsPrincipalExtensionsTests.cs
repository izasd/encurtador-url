using System.Security.Claims;
using Encurtador.Api.Usuarios;

namespace Encurtador.Tests.Usuarios;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void ObterIdDoUsuario_ComClaimSub_RetornaOId()
    {
        var usuario = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "42")], "Bearer"));

        Assert.Equal(42, usuario.ObterIdDoUsuario());
    }

    [Fact]
    public void ObterIdDoUsuario_SemClaimSub_LancaExcecao()
    {
        var usuario = new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", "ana@exemplo.com")], "Bearer"));

        Assert.Throws<InvalidOperationException>(() => usuario.ObterIdDoUsuario());
    }
}
