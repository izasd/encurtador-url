namespace Encurtador.Api.Cliques;

public class Click
{
    public const int TamanhoMaximoNavegador = 30;
    public const int TamanhoMaximoDispositivo = 20;
    public const int TamanhoMaximoReferer = 255;

    public long Id { get; set; }
    public int LinkId { get; set; }
    public DateTime CriadoEm { get; set; }

    /// <summary>Código ISO de duas letras (ex.: BR), quando conhecido.</summary>
    public string? Pais { get; set; }

    public required string Navegador { get; set; }
    public required string Dispositivo { get; set; }

    /// <summary>Domínio de onde o clique veio (ex.: t.co); nulo em acessos diretos.</summary>
    public string? Referer { get; set; }
}
