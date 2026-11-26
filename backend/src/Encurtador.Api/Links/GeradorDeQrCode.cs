using QRCoder;

namespace Encurtador.Api.Links;

public static class GeradorDeQrCode
{
    public const int TamanhoMinimo = 2;
    public const int TamanhoMaximo = 40;
    public const int TamanhoPadrao = 10;

    /// <summary>
    /// PNG do QR Code de um texto, com correção de erros nível M (até ~15% do código pode estar
    /// danificado) e a borda branca que os leitores precisam para encontrar o código.
    /// </summary>
    /// <param name="pixelsPorModulo">Tamanho de cada quadradinho do código, em pixels.</param>
    public static byte[] Gerar(string texto, int pixelsPorModulo = TamanhoPadrao)
    {
        using var gerador = new QRCodeGenerator();
        using var dados = gerador.CreateQrCode(texto, QRCodeGenerator.ECCLevel.M);

        return new PngByteQRCode(dados).GetGraphic(pixelsPorModulo);
    }
}
