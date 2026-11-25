using Encurtador.Api.Links;

namespace Encurtador.Tests.Links;

public class ValidadorDeCodigoTests
{
    [Theory]
    [InlineData("abcd")]
    [InlineData("1234")]
    [InlineData("AbC123")]
    [InlineData("abcdefghij")]
    public void EhValido_LetrasENumerosDe4a10Caracteres_RetornaTrue(string codigo)
    {
        Assert.True(ValidadorDeCodigo.EhValido(codigo));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("abcdefghijk")]
    [InlineData("ab-cd")]
    [InlineData("ab cd")]
    [InlineData("abc_1")]
    [InlineData("abc.1")]
    [InlineData("ação1")]
    [InlineData("ａｂｃｄ")]
    public void EhValido_TamanhoForaDoLimiteOuCaractereInvalido_RetornaFalse(string? codigo)
    {
        Assert.False(ValidadorDeCodigo.EhValido(codigo));
    }
}
