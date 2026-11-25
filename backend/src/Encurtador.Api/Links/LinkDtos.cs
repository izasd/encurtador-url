namespace Encurtador.Api.Links;

/// <param name="ExpiraEm">Opcional: data ISO 8601 com fuso a partir da qual o link para de redirecionar.</param>
public record CriarLinkRequest(string? UrlOriginal, string? Codigo = null, string? ExpiraEm = null);

public record LinkResponse(
    string Codigo,
    string UrlOriginal,
    string UrlCurta,
    DateTime CriadoEm,
    bool Ativo,
    DateTime? ExpiraEm,
    SituacaoDoLink Situacao);
