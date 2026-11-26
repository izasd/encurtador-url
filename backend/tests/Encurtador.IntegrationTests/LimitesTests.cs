using System.Net;
using System.Net.Http.Json;
using Encurtador.IntegrationTests.Infraestrutura;

namespace Encurtador.IntegrationTests;

[Collection(ColecaoDaApi.Nome)]
public class LimitesTests(ApiFactory api)
{
    [Fact]
    public async Task Login_MaisDe10TentativasPorMinutoDoMesmoIp_Retorna429SoParaEsseIp()
    {
        var cliente = api.CriarCliente();
        var tentativa = new { email = ClienteDaApi.EmailUnico(), senha = "senha-errada-123" };

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsJsonAsync("/api/auth/login", tentativa)).StatusCode);
        }

        var bloqueada = await cliente.PostAsJsonAsync("/api/auth/login", tentativa);
        var problema = await ClienteDaApi.LerProblemaAsync(bloqueada, HttpStatusCode.TooManyRequests);
        Assert.StartsWith("Muitas requisições em pouco tempo. Tente de novo em", problema.Detail);
        Assert.InRange(bloqueada.Headers.RetryAfter?.Delta ?? TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));

        var outroIp = api.CriarCliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await outroIp.PostAsJsonAsync("/api/auth/login", tentativa)).StatusCode);
    }

    [Fact]
    public async Task CriarLinks_MaisDe30PorMinutoDoMesmoUsuario_Retorna429SoParaEsseUsuario()
    {
        var cliente = await api.CriarUsuarioAsync();

        for (var i = 0; i < 30; i++)
        {
            await cliente.CriarLinkAsync();
        }

        var bloqueada = await cliente.PostAsJsonAsync("/api/links", new { urlOriginal = "https://exemplo.com" });
        await ClienteDaApi.LerProblemaAsync(bloqueada, HttpStatusCode.TooManyRequests);

        await (await api.CriarUsuarioAsync()).CriarLinkAsync();
    }

    [Fact]
    public async Task Visitante_MaisDe10ContasPorHoraDoMesmoIp_Retorna429()
    {
        var cliente = api.CriarCliente();

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync("/api/auth/visitante", content: null)).StatusCode);
        }

        var bloqueada = await cliente.PostAsync("/api/auth/visitante", content: null);
        await ClienteDaApi.LerProblemaAsync(bloqueada, HttpStatusCode.TooManyRequests);
        Assert.InRange(bloqueada.Headers.RetryAfter?.Delta ?? TimeSpan.Zero, TimeSpan.FromMinutes(59), TimeSpan.FromHours(1));
    }
}
