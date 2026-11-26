using Encurtador.Api.Usuarios;

namespace Encurtador.Tests.Usuarios;

public class RegrasDeContaTests
{
    [Theory]
    [InlineData("ana@exemplo.com", "ana@exemplo.com")]
    [InlineData("Ana.Souza@Exemplo.COM", "ana.souza@exemplo.com")]
    [InlineData("  ana@exemplo.com  ", "ana@exemplo.com")]
    public void NormalizarEmail_EmailValido_RetornaEmMinusculasEAparado(string email, string esperado)
    {
        Assert.Equal(esperado, RegrasDeConta.NormalizarEmail(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("@exemplo.com")]
    [InlineData("ana@@exemplo.com")]
    [InlineData("Ana <ana@exemplo.com>")]
    public void NormalizarEmail_EmailInvalido_RetornaNulo(string? email)
    {
        Assert.Null(RegrasDeConta.NormalizarEmail(email));
    }

    [Fact]
    public void NormalizarEmail_EmailAcimaDoTamanhoMaximo_RetornaNulo()
    {
        var email = new string('a', User.TamanhoMaximoEmail - "@exemplo.com".Length + 1) + "@exemplo.com";

        Assert.Null(RegrasDeConta.NormalizarEmail(email));
    }

    [Theory]
    [InlineData("Ana", "Ana")]
    [InlineData("  Ana Souza  ", "Ana Souza")]
    public void NormalizarNome_NomeValido_RetornaAparado(string nome, string esperado)
    {
        Assert.Equal(esperado, RegrasDeConta.NormalizarNome(nome));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizarNome_NomeVazio_RetornaNulo(string? nome)
    {
        Assert.Null(RegrasDeConta.NormalizarNome(nome));
    }

    [Fact]
    public void NormalizarNome_NomeAcimaDoTamanhoMaximo_RetornaNulo()
    {
        Assert.Null(RegrasDeConta.NormalizarNome(new string('a', User.TamanhoMaximoNome + 1)));
    }

    [Theory]
    [InlineData(RegrasDeConta.TamanhoMinimoSenha, true)]
    [InlineData(RegrasDeConta.TamanhoMaximoSenha, true)]
    [InlineData(RegrasDeConta.TamanhoMinimoSenha - 1, false)]
    [InlineData(RegrasDeConta.TamanhoMaximoSenha + 1, false)]
    public void SenhaValida_RespeitaOsLimitesDeTamanho(int tamanho, bool esperado)
    {
        Assert.Equal(esperado, RegrasDeConta.SenhaValida(new string('x', tamanho)));
    }

    [Fact]
    public void SenhaValida_SenhaNula_RetornaFalse()
    {
        Assert.False(RegrasDeConta.SenhaValida(null));
    }
}
