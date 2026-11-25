namespace Encurtador.Api.Cliques;

/// <summary>
/// Dados brutos de um clique, capturados no redirecionamento sem nenhum processamento.
/// </summary>
public record CliqueRegistrado(int LinkId, DateTime CriadoEm, string? UserAgent, string? Referer, string? Pais)
{
    public Click ParaClick()
    {
        var (navegador, dispositivo) = AnalisadorDeAcesso.ClassificarUserAgent(UserAgent);

        return new Click
        {
            LinkId = LinkId,
            CriadoEm = CriadoEm,
            Navegador = navegador,
            Dispositivo = dispositivo,
            Referer = AnalisadorDeAcesso.ExtrairDominio(Referer),
            Pais = AnalisadorDeAcesso.ExtrairPais(Pais),
        };
    }
}
