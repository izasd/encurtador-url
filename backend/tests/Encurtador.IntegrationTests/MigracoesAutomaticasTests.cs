using System.Net;
using Encurtador.Api.Dados;
using Encurtador.IntegrationTests.Infraestrutura;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Encurtador.IntegrationTests;

[Collection(ColecaoDaApi.Nome)]
public class MigracoesAutomaticasTests(ApiFactory api)
{
    [Fact]
    public async Task AoIniciar_ComAConfiguracaoLigada_CriaAsTabelasNumBancoVazio()
    {
        var bancoVazio = await CriarBancoVazioAsync();

        await using var novaApi = api.WithWebHostBuilder(host =>
        {
            host.UseSetting("ConnectionStrings:Encurtador", bancoVazio);
            host.UseSetting(MigracoesAutomaticas.Chave, "true");
        });
        var cliente = novaApi.CreateClient(); // sobe a API

        await using var db = ApiFactory.CriarDbContext(bancoVazio);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/saude")).StatusCode);
    }

    [Fact]
    public async Task AoIniciar_SemAConfiguracao_NaoMexeNoBanco()
    {
        var bancoVazio = await CriarBancoVazioAsync();

        await using var novaApi = api.WithWebHostBuilder(host => host.UseSetting("ConnectionStrings:Encurtador", bancoVazio));
        novaApi.CreateClient();

        await using var db = ApiFactory.CriarDbContext(bancoVazio);
        Assert.Empty(await db.Database.GetAppliedMigrationsAsync());
    }

    private async Task<string> CriarBancoVazioAsync()
    {
        var conexao = new NpgsqlConnectionStringBuilder(api.ConnectionString) { Database = $"vazio_{Guid.NewGuid():N}" };

        await using var principal = new NpgsqlConnection(api.ConnectionString);
        await principal.OpenAsync();
        await using var comando = new NpgsqlCommand($"CREATE DATABASE \"{conexao.Database}\"", principal);
        await comando.ExecuteNonQueryAsync();

        return conexao.ConnectionString;
    }
}
