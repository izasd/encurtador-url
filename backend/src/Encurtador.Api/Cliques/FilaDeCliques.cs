using System.Threading.Channels;

namespace Encurtador.Api.Cliques;

/// <summary>
/// Fila em memória entre o redirecionamento, que só enfileira, e o <see cref="GravadorDeCliques"/>,
/// que grava no banco em segundo plano. Assim o redirecionamento não espera o INSERT.
/// </summary>
public class FilaDeCliques(ILogger<FilaDeCliques> logger, int capacidade = FilaDeCliques.CapacidadePadrao)
{
    public const int CapacidadePadrao = 10_000;

    private readonly Channel<CliqueRegistrado> _canal = Channel.CreateBounded<CliqueRegistrado>(
        new BoundedChannelOptions(capacidade) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

    public ChannelReader<CliqueRegistrado> Leitor => _canal.Reader;

    /// <summary>
    /// Enfileira sem esperar. Se a fila estiver cheia (banco fora do ar ou lento demais),
    /// o clique é descartado: perder uma estatística é melhor que atrasar o redirecionamento.
    /// </summary>
    public bool TentarEnfileirar(CliqueRegistrado clique)
    {
        if (_canal.Writer.TryWrite(clique))
        {
            return true;
        }

        logger.LogWarning("Fila de cliques cheia; o clique do link {LinkId} foi descartado.", clique.LinkId);
        return false;
    }
}
