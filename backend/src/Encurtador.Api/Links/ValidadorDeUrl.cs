using System.Text;

namespace Encurtador.Api.Links;

public static class ValidadorDeUrl
{
    /// <summary>
    /// Valida a URL (absoluta, http ou https) e devolve a forma normalizada que deve ser salva.
    /// </summary>
    public static bool TentarNormalizar(string? entrada, out string urlNormalizada)
    {
        urlNormalizada = string.Empty;

        if (!Uri.TryCreate(entrada?.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        // A URL vai no cabeçalho Location do redirecionamento, que só aceita ASCII.
        // AbsoluteUri já codifica acentos, espaços e quebras de linha ("ação" vira
        // "a%C3%A7%C3%A3o"), mas mantém domínios com acento em Unicode; para eles
        // o IdnHost traz a versão em punycode ("müller.de" vira "xn--mller-kva.de").
        var normalizada = uri.AbsoluteUri;
        if (!Ascii.IsValid(normalizada))
        {
            normalizada = new UriBuilder(uri) { Host = uri.IdnHost }.Uri.AbsoluteUri;
        }

        // O limite vale para a forma normalizada, que é a que vai para o banco.
        if (normalizada.Length > Link.TamanhoMaximoUrl || !Ascii.IsValid(normalizada))
        {
            return false;
        }

        urlNormalizada = normalizada;
        return true;
    }
}
