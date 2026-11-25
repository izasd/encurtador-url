using Encurtador.Api.Links;

namespace Encurtador.Tests.Links;

public class LinkTests
{
    private static readonly DateTime Agora = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Situacao_AtivoSemExpiracao_EstaDisponivel()
    {
        Assert.Equal(SituacaoDoLink.Disponivel, Link.Situacao(true, null, Agora));
    }

    [Fact]
    public void Situacao_AtivoComExpiracaoFutura_EstaDisponivel()
    {
        Assert.Equal(SituacaoDoLink.Disponivel, Link.Situacao(true, Agora.AddSeconds(1), Agora));
    }

    [Fact]
    public void Situacao_ExpiracaoChegou_EstaExpirado()
    {
        Assert.Equal(SituacaoDoLink.Expirado, Link.Situacao(true, Agora, Agora));
        Assert.Equal(SituacaoDoLink.Expirado, Link.Situacao(true, Agora.AddDays(-1), Agora));
    }

    [Fact]
    public void Situacao_Desativado_PrevaleceSobreAExpiracao()
    {
        Assert.Equal(SituacaoDoLink.Desativado, Link.Situacao(false, null, Agora));
        Assert.Equal(SituacaoDoLink.Desativado, Link.Situacao(false, Agora.AddDays(-1), Agora));
    }
}
