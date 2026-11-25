using Encurtador.Api.Links;

namespace Encurtador.Tests.Links;

public class ValidadorDeUrlTests
{
    [Theory]
    [InlineData("http://exemplo.com")]
    [InlineData("https://exemplo.com/caminho?busca=1#ancora")]
    [InlineData("HTTPS://EXEMPLO.COM")]
    public void EhValida_UrlHttpOuHttps_RetornaTrue(string url)
    {
        Assert.True(ValidadorDeUrl.EhValida(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("exemplo.com")]
    [InlineData("/caminho/relativo")]
    [InlineData("http://")]
    [InlineData("ftp://exemplo.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:alguem@exemplo.com")]
    public void EhValida_UrlAusenteOuSemHttp_RetornaFalse(string? url)
    {
        Assert.False(ValidadorDeUrl.EhValida(url));
    }

    [Fact]
    public void EhValida_UrlNoTamanhoMaximo_RetornaTrue()
    {
        const string prefixo = "https://exemplo.com/";
        var url = prefixo + new string('a', Link.TamanhoMaximoUrl - prefixo.Length);

        Assert.True(ValidadorDeUrl.EhValida(url));
    }

    [Fact]
    public void EhValida_UrlAcimaDoTamanhoMaximo_RetornaFalse()
    {
        const string prefixo = "https://exemplo.com/";
        var url = prefixo + new string('a', Link.TamanhoMaximoUrl - prefixo.Length + 1);

        Assert.False(ValidadorDeUrl.EhValida(url));
    }
}
