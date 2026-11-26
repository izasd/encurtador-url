namespace Encurtador.Api.Links;

public class Link
{
    public const int TamanhoMinimoCodigo = 4;
    public const int TamanhoMaximoCodigo = 10;
    public const int TamanhoMaximoUrl = 2048;

    public int Id { get; set; }

    /// <summary>Dono do link: só ele lista o link e vê as estatísticas.</summary>
    public int UserId { get; set; }

    public required string Codigo { get; set; }
    public required string UrlOriginal { get; set; }
    public DateTime CriadoEm { get; set; }

    /// <summary>Desativado pelo dono, o link para de redirecionar até ser ativado de novo.</summary>
    public bool Ativo { get; set; } = true;

    /// <summary>A partir deste instante (UTC) o link para de redirecionar. Nulo: não expira.</summary>
    public DateTime? ExpiraEm { get; set; }

    public static SituacaoDoLink Situacao(bool ativo, DateTime? expiraEm, DateTime agora)
    {
        if (!ativo)
        {
            return SituacaoDoLink.Desativado;
        }

        return expiraEm <= agora ? SituacaoDoLink.Expirado : SituacaoDoLink.Disponivel;
    }
}

public enum SituacaoDoLink
{
    Disponivel,
    Desativado,
    Expirado,
}
