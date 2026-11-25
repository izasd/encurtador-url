using Microsoft.AspNetCore.HttpOverrides;

namespace Encurtador.Api.Hospedagem;

/// <summary>
/// Na hospedagem, um proxy reverso recebe a conexão do visitante (com HTTPS) e a repassa para a
/// API. O IP do visitante e o "https" original chegam nos cabeçalhos X-Forwarded-For e
/// X-Forwarded-Proto, que só são lidos com a seção "ProxyReverso" ligada. Ligue apenas quando
/// o proxy for o único caminho até a API: com acesso direto, qualquer um forjaria o próprio IP
/// e escaparia dos limites de requisição.
/// </summary>
public static class ProxyReverso
{
    public const string Secao = "ProxyReverso";

    public static IServiceCollection AddProxyReverso(this IServiceCollection services, IConfiguration configuracao)
    {
        var secao = configuracao.GetSection(Secao);

        services.Configure<ForwardedHeadersOptions>(opcoes =>
        {
            opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // O endereço do proxy da hospedagem muda e não é publicado, então não dá para listá-lo.
            opcoes.KnownIPNetworks.Clear();
            opcoes.KnownProxies.Clear();

            // Cada proxy acrescenta um IP no fim do X-Forwarded-For, e o visitante pode ter enviado
            // valores falsos no começo. Lendo da direita para a esquerda só tantos IPs quantos forem
            // os proxies da hospedagem ("Saltos"), chega-se ao IP real sem confiar no visitante.
            opcoes.ForwardLimit = secao.GetValue("Saltos", 1);

            // Alguns proxies entregam o IP do visitante em um cabeçalho próprio (ex.: CF-Connecting-IP).
            opcoes.ForwardedForHeaderName = secao["CabecalhoDoIp"] ?? ForwardedHeadersDefaults.XForwardedForHeaderName;
        });

        return services;
    }

    /// <summary>Deve ser o primeiro middleware, para todos os outros já verem o IP e o esquema reais.</summary>
    public static IApplicationBuilder UseProxyReverso(this WebApplication app) =>
        app.Configuration.GetValue<bool>($"{Secao}:Habilitado") ? app.UseForwardedHeaders() : app;
}
