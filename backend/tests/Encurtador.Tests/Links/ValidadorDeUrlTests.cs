using Encurtador.Api.Links;

namespace Encurtador.Tests.Links;

public class ValidadorDeUrlTests
{
    [Theory]
    [InlineData("http://exemplo.com/", "http://exemplo.com/")]
    [InlineData("https://exemplo.com/caminho?busca=1#ancora", "https://exemplo.com/caminho?busca=1#ancora")]
    [InlineData("https://exemplo.com", "https://exemplo.com/")]
    [InlineData("  https://exemplo.com/caminho  ", "https://exemplo.com/caminho")]
    [InlineData("HTTPS://Exemplo.COM/Caminho", "https://exemplo.com/Caminho")]
    public void TentarNormalizar_UrlHttpOuHttps_RetornaUrlNormalizada(string entrada, string esperada)
    {
        Assert.True(ValidadorDeUrl.TentarNormalizar(entrada, out var normalizada));
        Assert.Equal(esperada, normalizada);
    }

    [Theory]
    [InlineData("https://exemplo.com/ação?busca=é", "https://exemplo.com/a%C3%A7%C3%A3o?busca=%C3%A9")]
    [InlineData("https://exemplo.com/a b", "https://exemplo.com/a%20b")]
    [InlineData("https://müller.de/página", "https://xn--mller-kva.de/p%C3%A1gina")]
    [InlineData("https://exemplo.com/a\r\nSet-Cookie: x=1", "https://exemplo.com/a%0D%0ASet-Cookie:%20x=1")]
    public void TentarNormalizar_UrlComCaracteresNaoAscii_CodificaParaAscii(string entrada, string esperada)
    {
        Assert.True(ValidadorDeUrl.TentarNormalizar(entrada, out var normalizada));
        Assert.Equal(esperada, normalizada);
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
    public void TentarNormalizar_UrlAusenteOuSemHttp_RetornaFalse(string? entrada)
    {
        Assert.False(ValidadorDeUrl.TentarNormalizar(entrada, out _));
    }

    [Fact]
    public void TentarNormalizar_UrlNoTamanhoMaximo_RetornaTrue()
    {
        const string prefixo = "https://exemplo.com/";
        var url = prefixo + new string('a', Link.TamanhoMaximoUrl - prefixo.Length);

        Assert.True(ValidadorDeUrl.TentarNormalizar(url, out _));
    }

    [Fact]
    public void TentarNormalizar_UrlAcimaDoTamanhoMaximo_RetornaFalse()
    {
        const string prefixo = "https://exemplo.com/";
        var url = prefixo + new string('a', Link.TamanhoMaximoUrl - prefixo.Length + 1);

        Assert.False(ValidadorDeUrl.TentarNormalizar(url, out _));
    }

    [Fact]
    public void TentarNormalizar_UrlQueUltrapassaOLimiteDepoisDeCodificada_RetornaFalse()
    {
        // Cada "ç" vira "%C3%A7" (6 caracteres) na forma normalizada.
        const string prefixo = "https://exemplo.com/";
        var url = prefixo + new string('ç', (Link.TamanhoMaximoUrl - prefixo.Length) / 2);

        Assert.True(url.Length <= Link.TamanhoMaximoUrl);
        Assert.False(ValidadorDeUrl.TentarNormalizar(url, out _));
    }
}
