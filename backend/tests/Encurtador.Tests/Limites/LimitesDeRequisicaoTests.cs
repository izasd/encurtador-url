using Encurtador.Api.Limites;

namespace Encurtador.Tests.Limites;

public class LimitesDeRequisicaoTests
{
    [Theory]
    [InlineData(0.4, "1 segundo")]
    [InlineData(40, "40 segundos")]
    [InlineData(59.2, "1 minuto")]
    [InlineData(60, "1 minuto")]
    [InlineData(61, "2 minutos")]
    [InlineData(3_480, "58 minutos")]
    public void DescreverEspera_ArredondaParaCimaEmSegundosOuMinutos(double segundos, string esperado)
    {
        Assert.Equal(esperado, LimitesDeRequisicao.DescreverEspera(TimeSpan.FromSeconds(segundos)));
    }
}
