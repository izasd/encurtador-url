using Encurtador.Api.Links;

namespace Encurtador.Tests.Links;

public class GeradorDeCodigoTests
{
    [Fact]
    public void Gerar_SemInformarTamanho_GeraCodigoComTamanhoPadrao()
    {
        var codigo = GeradorDeCodigo.Gerar();

        Assert.Equal(GeradorDeCodigo.TamanhoPadrao, codigo.Length);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(10)]
    public void Gerar_InformandoTamanho_RespeitaOTamanho(int tamanho)
    {
        var codigo = GeradorDeCodigo.Gerar(tamanho);

        Assert.Equal(tamanho, codigo.Length);
    }

    [Fact]
    public void Gerar_UsaApenasLetrasENumeros()
    {
        for (var i = 0; i < 1_000; i++)
        {
            Assert.Matches("^[A-Za-z0-9]+$", GeradorDeCodigo.Gerar());
        }
    }
}
