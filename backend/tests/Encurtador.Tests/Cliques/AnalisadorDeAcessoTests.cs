using Encurtador.Api.Cliques;

namespace Encurtador.Tests.Cliques;

public class AnalisadorDeAcessoTests
{
    [Theory]
    // Computador
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36", "Chrome", "Computador")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0", "Edge", "Computador")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 OPR/116.0.0.0", "Opera", "Computador")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64; rv:133.0) Gecko/20100101 Firefox/133.0", "Firefox", "Computador")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Safari/605.1.15", "Safari", "Computador")]
    // Celular
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1", "Safari", "Celular")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/131.0.6778.73 Mobile/15E148 Safari/604.1", "Chrome", "Celular")]
    [InlineData("Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36", "Chrome", "Celular")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SAMSUNG SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/26.0 Chrome/122.0.0.0 Mobile Safari/537.36", "Samsung Internet", "Celular")]
    [InlineData("Mozilla/5.0 (Android 14; Mobile; rv:133.0) Gecko/133.0 Firefox/133.0", "Firefox", "Celular")]
    // Tablet
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1", "Safari", "Tablet")]
    [InlineData("Mozilla/5.0 (Linux; Android 13; SM-X700) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36", "Chrome", "Tablet")]
    // Robôs e clientes HTTP
    [InlineData("WhatsApp/2.23.20.0 A", "Robô", "Robô")]
    [InlineData("facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)", "Robô", "Robô")]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)", "Robô", "Robô")]
    [InlineData("TelegramBot (like TwitterBot)", "Robô", "Robô")]
    [InlineData("curl/8.10.1", "Robô", "Robô")]
    // Desconhecidos
    [InlineData("MeuAppInterno/1.0", "Outro", "Desconhecido")]
    public void ClassificarUserAgent_IdentificaNavegadorEDispositivo(string userAgent, string navegador, string dispositivo)
    {
        var resultado = AnalisadorDeAcesso.ClassificarUserAgent(userAgent);

        Assert.Equal((navegador, dispositivo), resultado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ClassificarUserAgent_SemUserAgent_RetornaDesconhecido(string? userAgent)
    {
        var resultado = AnalisadorDeAcesso.ClassificarUserAgent(userAgent);

        Assert.Equal((AnalisadorDeAcesso.Desconhecido, AnalisadorDeAcesso.Desconhecido), resultado);
    }

    [Theory]
    [InlineData("https://t.co/abc123", "t.co")]
    [InlineData("https://www.google.com/", "www.google.com")]
    [InlineData("http://Exemplo.COM/pagina?token=secreto", "exemplo.com")]
    public void ExtrairDominio_RefererValido_RetornaSoODominio(string referer, string dominio)
    {
        Assert.Equal(dominio, AnalisadorDeAcesso.ExtrairDominio(referer));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nao-e-url")]
    [InlineData("android-app://com.google.android.gm/")]
    public void ExtrairDominio_RefererAusenteOuInvalido_RetornaNulo(string? referer)
    {
        Assert.Null(AnalisadorDeAcesso.ExtrairDominio(referer));
    }

    [Theory]
    [InlineData("BR", "BR")]
    [InlineData("br", "BR")]
    [InlineData(" PT ", "PT")]
    public void ExtrairPais_CodigoDeDuasLetras_RetornaEmMaiusculas(string codigo, string esperado)
    {
        Assert.Equal(esperado, AnalisadorDeAcesso.ExtrairPais(codigo));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("XX")]
    [InlineData("T1")]
    [InlineData("BRA")]
    public void ExtrairPais_CodigoAusenteOuDesconhecido_RetornaNulo(string? codigo)
    {
        Assert.Null(AnalisadorDeAcesso.ExtrairPais(codigo));
    }
}
