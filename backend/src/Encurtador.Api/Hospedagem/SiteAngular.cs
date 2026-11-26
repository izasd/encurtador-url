using Microsoft.AspNetCore.StaticFiles;

namespace Encurtador.Api.Hospedagem;

/// <summary>
/// Serve o site Angular publicado junto com a API, na pasta wwwroot/app (o Dockerfile copia o
/// build do frontend para lá). O site fica em /app/ porque, na raiz, qualquer caminho de 4 a 10
/// letras e números é um link curto: /links ou /entrar seriam confundidos com códigos.
/// Em desenvolvimento a pasta não existe e o site continua rodando no ng serve.
/// </summary>
public static class SiteAngular
{
    public const string Caminho = "/app";

    private const string Index = "app/index.html";

    private static readonly StaticFileOptions Arquivos = new() { OnPrepareResponse = DefinirCache };

    public static IApplicationBuilder UseSiteAngular(this WebApplication app) =>
        EstaPublicado(app) ? app.UseStaticFiles(Arquivos) : app;

    public static IEndpointRouteBuilder MapSiteAngular(this WebApplication app)
    {
        if (!EstaPublicado(app))
        {
            return app;
        }

        app.MapGet("/", () => TypedResults.Redirect($"{Caminho}/")).ExcludeFromDescription();

        // /app, /app/ e as rotas do Angular (/app/links, /app/entrar...) recebem o index.html, e o
        // roteador do Angular mostra a página certa. Arquivos inexistentes (/app/x.js) dão 404.
        app.MapFallbackToFile($"{Caminho}/{{**rota:nonfile}}", Index, Arquivos);

        return app;
    }

    private static bool EstaPublicado(WebApplication app) =>
        app.Environment.WebRootFileProvider.GetFileInfo(Index).Exists;

    private static void DefinirCache(StaticFileResponseContext contexto)
    {
        var nome = contexto.File.Name;

        contexto.Context.Response.Headers.CacheControl = nome switch
        {
            // Muda a cada versão e aponta para os arquivos novos: o navegador sempre confere.
            "index.html" => "no-cache",

            // O Angular põe o hash do conteúdo no nome (main-ABCD1234.js): uma versão nova tem outro nome.
            _ when nome.EndsWith(".js", StringComparison.Ordinal) || nome.EndsWith(".css", StringComparison.Ordinal)
                => "public, max-age=31536000, immutable",

            _ => "public, max-age=3600",
        };
    }
}
