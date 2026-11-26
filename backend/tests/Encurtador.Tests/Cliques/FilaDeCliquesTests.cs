using Encurtador.Api.Cliques;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encurtador.Tests.Cliques;

public class FilaDeCliquesTests
{
    private static readonly CliqueRegistrado Clique = new(1, DateTime.UtcNow, null, null, null);

    [Fact]
    public void TentarEnfileirar_ComEspaco_DeixaOCliqueDisponivelParaLeitura()
    {
        var fila = new FilaDeCliques(NullLogger<FilaDeCliques>.Instance);

        Assert.True(fila.TentarEnfileirar(Clique));
        Assert.True(fila.Leitor.TryRead(out var lido));
        Assert.Equal(Clique, lido);
    }

    [Fact]
    public void TentarEnfileirar_FilaCheia_DescartaSemEsperar()
    {
        var fila = new FilaDeCliques(NullLogger<FilaDeCliques>.Instance, capacidade: 2);

        Assert.True(fila.TentarEnfileirar(Clique));
        Assert.True(fila.TentarEnfileirar(Clique));
        Assert.False(fila.TentarEnfileirar(Clique));
    }
}
