namespace Encurtador.IntegrationTests.Infraestrutura;

/// <summary>
/// As classes de teste desta coleção compartilham a mesma API e o mesmo contêiner do PostgreSQL.
/// Cada teste cria os próprios usuários e links, então eles não dependem uns dos outros.
/// </summary>
[CollectionDefinition(Nome)]
public class ColecaoDaApi : ICollectionFixture<ApiFactory>
{
    public const string Nome = "API com PostgreSQL";
}
