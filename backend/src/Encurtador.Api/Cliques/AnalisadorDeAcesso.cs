namespace Encurtador.Api.Cliques;

/// <summary>
/// Extrai navegador, dispositivo, domínio de origem e país dos dados de uma requisição.
/// É uma heurística simples, que cobre os navegadores e dispositivos mais comuns.
/// </summary>
public static class AnalisadorDeAcesso
{
    public const string Desconhecido = "Desconhecido";
    public const string Robo = "Robô";

    // Robôs de busca, de pré-visualização de links (WhatsApp, Facebook...) e clientes HTTP.
    private static readonly string[] MarcasDeRobo =
    [
        "bot", "crawl", "spider", "slurp", "facebookexternalhit", "whatsapp",
        "curl/", "wget/", "python-requests", "headlesschrome", "go-http-client", "postmanruntime",
    ];

    public static (string Navegador, string Dispositivo) ClassificarUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return (Desconhecido, Desconhecido);
        }

        if (Contem(userAgent, MarcasDeRobo))
        {
            return (Robo, Robo);
        }

        return (IdentificarNavegador(userAgent), IdentificarDispositivo(userAgent));
    }

    /// <summary>Devolve só o domínio do referer (ex.: "t.co"), ou nulo se não houver um válido.</summary>
    public static string? ExtrairDominio(string? referer) =>
        Uri.TryCreate(referer, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.Host.Length is > 0 and <= Click.TamanhoMaximoReferer
            ? uri.Host
            : null;

    /// <summary>
    /// Normaliza o código de país do cabeçalho CF-IPCountry, que o Cloudflare envia quando
    /// fica na frente da API. "XX" (desconhecido) e "T1" (rede Tor) viram nulo.
    /// </summary>
    public static string? ExtrairPais(string? codigo)
    {
        var normalizado = codigo?.Trim().ToUpperInvariant();

        return normalizado is { Length: 2 } && normalizado.All(char.IsAsciiLetterUpper) && normalizado != "XX"
            ? normalizado
            : null;
    }

    // A ordem importa: Edge e Opera também dizem "Chrome", e o Chrome também diz "Safari".
    private static string IdentificarNavegador(string ua) =>
        Contem(ua, "Edg/", "Edge/", "EdgA/", "EdgiOS/") ? "Edge"
        : Contem(ua, "OPR/", "Opera") ? "Opera"
        : Contem(ua, "SamsungBrowser/") ? "Samsung Internet"
        : Contem(ua, "Firefox/", "FxiOS/") ? "Firefox"
        : Contem(ua, "Chrome/", "CriOS/", "Chromium/") ? "Chrome"
        : Contem(ua, "Safari/") ? "Safari"
        : "Outro";

    // O iPad também diz "Mobile", e tablets Android não dizem "Mobile": por isso o tablet vem antes.
    private static string IdentificarDispositivo(string ua) =>
        Contem(ua, "iPad", "Tablet") || (Contem(ua, "Android") && !Contem(ua, "Mobile")) ? "Tablet"
        : Contem(ua, "Mobi", "iPhone", "iPod", "Android") ? "Celular"
        : Contem(ua, "Windows", "Macintosh", "X11", "Linux", "CrOS") ? "Computador"
        : Desconhecido;

    private static bool Contem(string texto, params string[] trechos) =>
        trechos.Any(trecho => texto.Contains(trecho, StringComparison.OrdinalIgnoreCase));
}
