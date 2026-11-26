using System.Globalization;
using System.Text.RegularExpressions;

namespace Encurtador.Api.Links;

public static partial class ValidadorDeExpiracao
{
    public const string Mensagem =
        "Informe uma data futura no formato ISO 8601, com fuso horário (ex.: 2026-12-31T23:59:59-03:00).";

    private static readonly string[] Formatos =
    [
        "yyyy-MM-dd'T'HH:mmK",
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
    ];

    /// <summary>
    /// Lê a data de expiração e confere que ela está no futuro. O fuso é obrigatório: sem ele,
    /// "2026-12-31T23:59" seria interpretado no fuso do servidor, que não é o de quem usa a API.
    /// </summary>
    public static bool TentarLer(string? texto, DateTime agora, out DateTime expiraEm)
    {
        expiraEm = default;

        if (texto is null
            || !TerminaComFuso().IsMatch(texto)
            || !DateTimeOffset.TryParseExact(texto, Formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
        {
            return false;
        }

        expiraEm = data.UtcDateTime;
        return expiraEm > agora;
    }

    [GeneratedRegex(@"(Z|[+-]\d{2}:\d{2})$")]
    private static partial Regex TerminaComFuso();
}
