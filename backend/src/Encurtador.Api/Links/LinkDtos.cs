namespace Encurtador.Api.Links;

public record CriarLinkRequest(string? UrlOriginal, string? Codigo = null);

public record LinkResponse(string Codigo, string UrlOriginal, string UrlCurta, DateTime CriadoEm);
