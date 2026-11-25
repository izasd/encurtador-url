namespace Encurtador.Api.Cliques;

public record EstatisticasResponse(
    string Codigo,
    string FusoHorario,
    int TotalDeCliques,
    IReadOnlyList<CliquesPorDia> PorDia,
    IReadOnlyList<CliquesPorCategoria> PorPais,
    IReadOnlyList<CliquesPorCategoria> PorNavegador,
    IReadOnlyList<CliquesPorCategoria> PorDispositivo,
    IReadOnlyList<CliquesPorCategoria> PorReferer);

public record CliquesPorDia(DateOnly Dia, int Cliques);

public record CliquesPorCategoria(string Nome, int Cliques);
