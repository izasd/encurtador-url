using System.Linq.Expressions;
using Encurtador.Api.Dados;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Api.Cliques;

public static class EstatisticasEndpoints
{
    private const string SemReferer = "Direto";

    public static IEndpointRouteBuilder MapEstatisticasEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/links/{codigo:codigoCurto}/estatisticas", ObterEstatisticas)
            .WithTags("Estatísticas")
            .WithSummary("Cliques do link por dia, país, navegador, dispositivo e origem")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    /// <param name="fusoHorario">Fuso IANA usado para separar os dias (ex.: America/Sao_Paulo).</param>
    private static async Task<Results<Ok<EstatisticasResponse>, ValidationProblem, ProblemHttpResult>> ObterEstatisticas(
        string codigo, AppDbContext db, FusosHorarios fusos, CancellationToken ct, string fusoHorario = "UTC")
    {
        var linkId = await db.Links
            .Where(l => l.Codigo == codigo)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync(ct);

        if (linkId is null)
        {
            return TypedResults.Problem(
                detail: $"Nenhum link encontrado com o código '{codigo}'.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (!await fusos.ExisteAsync(fusoHorario, db, ct))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["fusoHorario"] = ["Informe um fuso horário IANA válido, como America/Sao_Paulo ou UTC."],
            });
        }

        var cliques = db.Clicks.Where(c => c.LinkId == linkId);

        // O PostgreSQL converte cada horário para o fuso pedido (AT TIME ZONE) antes de separar
        // os dias: um clique às 22h em Brasília conta naquele dia, e não no seguinte (como em UTC).
        var porDia = (await cliques
                .GroupBy(c => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(c.CriadoEm, fusoHorario).Date)
                .Select(g => new { Dia = g.Key, Cliques = g.Count() })
                .OrderBy(g => g.Dia)
                .ToListAsync(ct))
            .Select(g => new CliquesPorDia(DateOnly.FromDateTime(g.Dia), g.Cliques))
            .ToList();

        return TypedResults.Ok(new EstatisticasResponse(
            codigo,
            fusoHorario,
            TotalDeCliques: porDia.Sum(d => d.Cliques),
            porDia,
            PorPais: await ContarPorAsync(cliques, c => c.Pais, AnalisadorDeAcesso.Desconhecido, ct),
            PorNavegador: await ContarPorAsync(cliques, c => c.Navegador, AnalisadorDeAcesso.Desconhecido, ct),
            PorDispositivo: await ContarPorAsync(cliques, c => c.Dispositivo, AnalisadorDeAcesso.Desconhecido, ct),
            PorReferer: await ContarPorAsync(cliques, c => c.Referer, SemReferer, ct)));
    }

    private static async Task<List<CliquesPorCategoria>> ContarPorAsync(
        IQueryable<Click> cliques, Expression<Func<Click, string?>> categoria, string semValor, CancellationToken ct)
    {
        var grupos = await cliques
            .GroupBy(categoria)
            .Select(g => new { Nome = g.Key, Cliques = g.Count() })
            .ToListAsync(ct);

        return grupos
            .Select(g => new CliquesPorCategoria(g.Nome ?? semValor, g.Cliques))
            .OrderByDescending(g => g.Cliques)
            .ThenBy(g => g.Nome)
            .ToList();
    }
}
