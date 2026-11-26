using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Encurtador.Api.Links;

/// <param name="ExpiraEm">Opcional: data ISO 8601 com fuso a partir da qual o link para de redirecionar.</param>
public record CriarLinkRequest(string? UrlOriginal, string? Codigo = null, string? ExpiraEm = null);

/// <summary>
/// Corpo do PATCH no estilo JSON Merge Patch (RFC 7396): campo ausente não muda nada, e
/// "expiraEm": null remove a expiração. ExpiraEm é um JsonElement justamente para diferenciar
/// "ausente" (Undefined) de "null" (Null), o que um DateTime? não consegue.
/// </summary>
public record AtualizarLinkRequest(bool? Ativo, JsonElement ExpiraEm)
{
    /// <summary>Lê o pedido. Retorna false, com a mensagem de erro, se "expiraEm" for inválido.</summary>
    public bool TentarInterpretar(DateTime agora, out AlteracoesDoLink alteracoes, [NotNullWhen(false)] out string? erro)
    {
        alteracoes = new AlteracoesDoLink(Ativo, MudaExpiracao: false, ExpiraEm: null);
        erro = null;

        switch (ExpiraEm.ValueKind)
        {
            case JsonValueKind.Undefined:
                return true;

            case JsonValueKind.Null:
                alteracoes = alteracoes with { MudaExpiracao = true };
                return true;

            case JsonValueKind.String when ValidadorDeExpiracao.TentarLer(ExpiraEm.GetString(), agora, out var data):
                alteracoes = alteracoes with { MudaExpiracao = true, ExpiraEm = data };
                return true;

            default:
                erro = $"{ValidadorDeExpiracao.Mensagem} Para remover a expiração, envie null.";
                return false;
        }
    }
}

public record AlteracoesDoLink(bool? Ativo, bool MudaExpiracao, DateTime? ExpiraEm);

public record LinkResponse(
    string Codigo,
    string UrlOriginal,
    string UrlCurta,
    DateTime CriadoEm,
    bool Ativo,
    DateTime? ExpiraEm,
    SituacaoDoLink Situacao);
