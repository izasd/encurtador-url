using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Dados;

public static class MigracoesAutomaticas
{
    /// <summary>Liga a atualização automática do banco (variável de ambiente Banco__AplicarMigracoesAoIniciar=true).</summary>
    public const string Chave = "Banco:AplicarMigracoesAoIniciar";

    /// <summary>
    /// Leva o banco até a última migration antes de a API começar a atender. Fica ligado nos
    /// contêineres (docker compose e hospedagem), onde não há um passo separado para rodar
    /// "dotnet ef database update". Com o banco já atualizado, não faz nada.
    /// </summary>
    public static async Task AplicarMigracoesSeConfiguradoAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>(Chave))
        {
            return;
        }

        await using var escopo = app.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }
}
