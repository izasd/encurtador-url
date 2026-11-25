using System.Collections.Frozen;
using Encurtador.Api.Dados;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Cliques;

/// <summary>
/// Fusos horários que o PostgreSQL conhece (nomes IANA, como "America/Sao_Paulo").
/// A lista é lida do banco uma vez só, porque consultar pg_timezone_names leva dezenas de milissegundos.
/// </summary>
public class FusosHorarios
{
    private FrozenSet<string>? _nomes;

    public async Task<bool> ExisteAsync(string nome, AppDbContext db, CancellationToken ct)
    {
        // Se duas requisições chegarem juntas antes da primeira carga, as duas carregam: não há problema.
        _nomes ??= (await db.Database
                .SqlQuery<string>($"SELECT name AS \"Value\" FROM pg_timezone_names")
                .ToListAsync(ct))
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        return _nomes.Contains(nome);
    }
}
