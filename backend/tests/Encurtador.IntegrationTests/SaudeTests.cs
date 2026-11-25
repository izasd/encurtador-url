using System.Net;
using Encurtador.IntegrationTests.Infraestrutura;
using Microsoft.AspNetCore.Hosting;

namespace Encurtador.IntegrationTests;

[Collection(ColecaoDaApi.Nome)]
public class SaudeTests(ApiFactory api)
{
    [Fact]
    public async Task Saude_ComOBancoNoAr_Retorna200()
    {
        var resposta = await api.CriarCliente().GetAsync("/api/saude");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Healthy", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Saude_SemConseguirFalarComOBanco_Retorna503()
    {
        // Porta 1: nada escuta ali, então a conexão é recusada na hora.
        await using var semBanco = api.WithWebHostBuilder(host =>
            host.UseSetting("ConnectionStrings:Encurtador", "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x"));

        var resposta = await semBanco.CreateClient().GetAsync("/api/saude");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Equal("Unhealthy", await resposta.Content.ReadAsStringAsync());
    }
}
