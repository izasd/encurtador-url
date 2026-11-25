using System.Text.Json;
using Encurtador.Api.Links;

namespace Encurtador.Tests.Links;

public class AtualizarLinkRequestTests
{
    private static readonly DateTime Agora = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Lê o corpo como a API lê, para testar exatamente o que chega do JSON.</summary>
    private static AtualizarLinkRequest Ler(string json) =>
        JsonSerializer.Deserialize<AtualizarLinkRequest>(json, JsonSerializerOptions.Web)!;

    [Fact]
    public void CorpoVazio_NaoMudaNada()
    {
        Assert.True(Ler("{}").TentarInterpretar(Agora, out var alteracoes, out _));
        Assert.Equal(new AlteracoesDoLink(Ativo: null, MudaExpiracao: false, ExpiraEm: null), alteracoes);
    }

    [Fact]
    public void SoAtivo_MudaSoOAtivo()
    {
        Assert.True(Ler("""{ "ativo": false }""").TentarInterpretar(Agora, out var alteracoes, out _));
        Assert.Equal(new AlteracoesDoLink(Ativo: false, MudaExpiracao: false, ExpiraEm: null), alteracoes);
    }

    [Fact]
    public void ExpiraEmNulo_RemoveAExpiracao()
    {
        Assert.True(Ler("""{ "expiraEm": null }""").TentarInterpretar(Agora, out var alteracoes, out _));
        Assert.Equal(new AlteracoesDoLink(Ativo: null, MudaExpiracao: true, ExpiraEm: null), alteracoes);
    }

    [Fact]
    public void ExpiraEmComData_DefineAExpiracaoEmUtc()
    {
        var pedido = Ler("""{ "ativo": true, "expiraEm": "2026-12-31T23:59:59-03:00" }""");

        Assert.True(pedido.TentarInterpretar(Agora, out var alteracoes, out _));
        Assert.Equal(
            new AlteracoesDoLink(Ativo: true, MudaExpiracao: true, ExpiraEm: new DateTime(2027, 1, 1, 2, 59, 59, DateTimeKind.Utc)),
            alteracoes);
    }

    [Theory]
    [InlineData("""{ "expiraEm": "2020-01-01T00:00:00Z" }""")] // no passado
    [InlineData("""{ "expiraEm": "2030-01-01T00:00:00" }""")] // sem fuso
    [InlineData("""{ "expiraEm": 123 }""")] // não é texto
    [InlineData("""{ "expiraEm": "amanhã" }""")]
    public void ExpiraEmInvalido_RetornaErroExplicandoComoRemover(string json)
    {
        Assert.False(Ler(json).TentarInterpretar(Agora, out _, out var erro));
        Assert.Contains("envie null", erro);
    }
}
