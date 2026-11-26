using System.Buffers.Binary;
using Encurtador.Api.Links;

namespace Encurtador.Tests.Links;

public class GeradorDeQrCodeTests
{
    private const string Url = "http://localhost:5198/abc123";

    // Assinatura que abre todo arquivo PNG.
    private static readonly byte[] AssinaturaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Largura da imagem, lida do cabeçalho IHDR do PNG (bytes 16 a 19).</summary>
    private static int Largura(byte[] png) => BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));

    [Fact]
    public void Gerar_DevolveUmPng()
    {
        var png = GeradorDeQrCode.Gerar(Url);

        Assert.Equal(AssinaturaPng, png[..8]);
    }

    [Fact]
    public void Gerar_TamanhoMultiplicaOsPixelsDeCadaModulo()
    {
        var pequeno = GeradorDeQrCode.Gerar(Url, pixelsPorModulo: 5);
        var grande = GeradorDeQrCode.Gerar(Url, pixelsPorModulo: 10);

        Assert.Equal(2 * Largura(pequeno), Largura(grande));
        Assert.Equal(0, Largura(grande) % 10);
    }
}
