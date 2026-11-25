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
}
