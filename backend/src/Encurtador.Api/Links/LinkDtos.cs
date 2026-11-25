namespace Encurtador.Api.Links;

public record CriarLinkRequest(string? UrlOriginal);

public record LinkResponse(string Codigo, string UrlOriginal, string UrlCurta, DateTime CriadoEm);

public record RespostaErro(string Erro);
