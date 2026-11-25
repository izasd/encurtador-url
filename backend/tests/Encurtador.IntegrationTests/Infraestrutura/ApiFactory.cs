using Encurtador.Api.Dados;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Encurtador.IntegrationTests.Infraestrutura;

/// <summary>
/// Sobe a API inteira em memória (TestServer) ligada a um PostgreSQL de verdade, num contêiner
/// Docker criado pelo Testcontainers no início dos testes e descartado no fim.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>A mesma imagem do docker-compose.yml.</summary>
    public const string ImagemDoBanco = "postgres:18-alpine";

    private readonly PostgreSqlContainer _banco = new PostgreSqlBuilder(ImagemDoBanco).Build();

    private int _ultimoIp;

    public string ConnectionString => _banco.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _banco.StartAsync();

        await using var db = CriarDbContext(ConnectionString);
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _banco.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Produção, para não depender do appsettings.Development.json.
        builder.UseEnvironment(Environments.Production);
        builder.UseSetting("ConnectionStrings:Encurtador", ConnectionString);
        builder.UseSetting("Jwt:Chave", "chave-dos-testes-de-integracao-com-mais-de-32-caracteres");
        builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter, IpDoClienteDeTeste>());
    }

    /// <summary>
    /// Cliente HTTP com um endereço IP só dele, para que os limites por IP de um teste
    /// não interfiram nos outros. Não segue redirecionamentos, para os testes verem o 302.
    /// </summary>
    public HttpClient CriarCliente()
    {
        var cliente = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        cliente.DefaultRequestHeaders.Add(IpDoClienteDeTeste.Cabecalho, NovoIp());
        return cliente;
    }

    /// <summary>Acesso direto ao banco, para preparar cenários e conferir o que a API gravou.</summary>
    public AppDbContext CriarDbContext() => CriarDbContext(ConnectionString);

    public static AppDbContext CriarDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);

    private string NovoIp()
    {
        var n = Interlocked.Increment(ref _ultimoIp);
        return $"10.{(n >> 16) & 255}.{(n >> 8) & 255}.{n & 255}";
    }
}
