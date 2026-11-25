using System.Diagnostics.CodeAnalysis;

namespace Encurtador.Api.Links;

public static class ValidadorDeUrl
{
    public static bool EhValida([NotNullWhen(true)] string? url) =>
        url is { Length: <= Link.TamanhoMaximoUrl }
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
