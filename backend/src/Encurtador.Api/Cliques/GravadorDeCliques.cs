using Encurtador.Api.Dados;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Cliques;

/// <summary>
/// Lê a <see cref="FilaDeCliques"/> em segundo plano e grava os cliques no banco em lotes.
/// </summary>
public class GravadorDeCliques(
    FilaDeCliques fila, IServiceScopeFactory scopeFactory, ILogger<GravadorDeCliques> logger) : BackgroundService
{
    private const int TamanhoMaximoDoLote = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await fila.Leitor.WaitToReadAsync(stoppingToken))
            {
                await GravarLoteAsync();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // A aplicação está encerrando.
        }

        // Antes de encerrar, grava o que ainda estiver na fila.
        while (await GravarLoteAsync() > 0)
        {
        }
    }

    private async Task<int> GravarLoteAsync()
    {
        var cliques = new List<CliqueRegistrado>();
        while (cliques.Count < TamanhoMaximoDoLote && fila.Leitor.TryRead(out var clique))
        {
            cliques.Add(clique);
        }

        if (cliques.Count == 0)
        {
            return 0;
        }

        // Um erro aqui não pode derrubar o serviço: o que não for gravado é registrado no log e descartado.
        try
        {
            await SalvarAsync(cliques);
        }
        catch (DbUpdateException ex) when (cliques.Count > 1)
        {
            // Um único clique inválido derruba o lote inteiro: por exemplo, o clique de um link
            // apagado entre o redirecionamento e a gravação viola a chave estrangeira. Gravando um
            // por um, só esse clique é perdido.
            logger.LogWarning(ex, "Falha ao gravar um lote de {Quantidade} cliques; gravando um por um.", cliques.Count);

            foreach (var clique in cliques)
            {
                try
                {
                    await SalvarAsync([clique]);
                }
                catch (Exception exClique)
                {
                    logger.LogWarning(exClique, "Clique do link {LinkId} descartado.", clique.LinkId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Não foi possível gravar {Quantidade} cliques; eles foram descartados.", cliques.Count);
        }

        return cliques.Count;
    }

    private async Task SalvarAsync(IEnumerable<CliqueRegistrado> cliques)
    {
        await using var escopo = scopeFactory.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Clicks.AddRange(cliques.Select(c => c.ParaClick()));

        // Sem CancellationToken de propósito: cliques já retirados da fila devem terminar de ser gravados.
        await db.SaveChangesAsync();
    }
}
