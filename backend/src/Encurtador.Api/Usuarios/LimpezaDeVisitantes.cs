using Encurtador.Api.Dados;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Usuarios;

/// <summary>
/// Apaga, ao iniciar e depois a cada hora, as contas de visitante criadas há mais de 24 horas.
/// Os links e cliques dessas contas são apagados em cascata pelo banco.
/// </summary>
public class LimpezaDeVisitantes(
    IServiceScopeFactory scopeFactory, TimeProvider relogio, ILogger<LimpezaDeVisitantes> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo, relogio);

        try
        {
            do
            {
                await ApagarExpiradasAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // A aplicação está encerrando.
        }
    }

    private async Task ApagarExpiradasAsync(CancellationToken ct)
    {
        // Um erro aqui (ex.: banco fora do ar) não pode derrubar a API: tenta de novo na próxima hora.
        try
        {
            await using var escopo = scopeFactory.CreateAsyncScope();
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            var limite = relogio.GetUtcNow().UtcDateTime - DadosDeExemplo.Validade;

            var apagadas = await db.Users
                .Where(u => u.EhVisitante && u.CriadoEm < limite)
                .ExecuteDeleteAsync(ct);

            if (apagadas > 0)
            {
                logger.LogInformation("{Quantidade} contas de visitante expiradas foram apagadas.", apagadas);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao apagar as contas de visitante expiradas.");
        }
    }
}
