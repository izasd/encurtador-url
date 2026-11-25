using Encurtador.IntegrationTests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.IntegrationTests;

[Collection(ColecaoDaApi.Nome)]
public class BancoTests(ApiFactory api)
{
    [Fact]
    public async Task Migracoes_RodamNoPostgreSqlECobremTodoOModelo()
    {
        await using var db = api.CriarDbContext();

        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());

        // Falha se uma entidade mudou e ninguém gerou a migration correspondente.
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
