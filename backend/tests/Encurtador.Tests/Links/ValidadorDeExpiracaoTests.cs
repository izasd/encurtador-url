using Encurtador.Api.Links;

namespace Encurtador.Tests.Links;

public class ValidadorDeExpiracaoTests
{
    private static readonly DateTime Agora = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("2026-12-31T23:59:59Z", "2026-12-31T23:59:59")]
    [InlineData("2026-12-31T23:59:59-03:00", "2027-01-01T02:59:59")]
    [InlineData("2026-12-01T10:30+02:00", "2026-12-01T08:30:00")]
    [InlineData("2026-12-01T10:30:00.123Z", "2026-12-01T10:30:00.123")]
    public void TentarLer_DataFuturaComFuso_RetornaEmUtc(string texto, string esperadoUtc)
    {
        Assert.True(ValidadorDeExpiracao.TentarLer(texto, Agora, out var expiraEm));
        Assert.Equal(DateTime.Parse(esperadoUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal), expiraEm);
        Assert.Equal(DateTimeKind.Utc, expiraEm.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("amanhã")]
    [InlineData("2026-12-31T23:59:59")] // sem fuso
    [InlineData("31/12/2026 23:59 +00:00")] // fora do formato ISO 8601
    [InlineData("2026-11-25T12:00:00Z")] // agora não é futuro
    [InlineData("2026-11-24T12:00:00Z")] // passado
    public void TentarLer_DataInvalidaSemFusoOuNoPassado_RetornaFalse(string? texto)
    {
        Assert.False(ValidadorDeExpiracao.TentarLer(texto, Agora, out _));
    }
}
