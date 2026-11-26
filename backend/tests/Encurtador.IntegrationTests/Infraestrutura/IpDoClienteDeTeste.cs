using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Encurtador.IntegrationTests.Infraestrutura;

/// <summary>
/// No TestServer as requisições não têm IP de origem. Este filtro usa o IP informado no
/// cabeçalho <see cref="Cabecalho"/>, para os testes exercitarem os limites por IP.
/// </summary>
internal sealed class IpDoClienteDeTeste : IStartupFilter
{
    public const string Cabecalho = "X-Ip-De-Teste";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (http, proximo) =>
        {
            if (IPAddress.TryParse(http.Request.Headers[Cabecalho], out var ip))
            {
                http.Connection.RemoteIpAddress = ip;
            }

            await proximo(http);
        });

        next(app);
    };
}
