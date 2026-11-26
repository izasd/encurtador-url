using System.Security.Cryptography;

namespace Encurtador.Api.Links;

public static class GeradorDeCodigo
{
    public const int TamanhoPadrao = 6;

    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    // Gerador criptograficamente seguro: ao contrário de Random, não dá para prever
    // os próximos códigos a partir dos já gerados (o que permitiria varrer links).
    public static string Gerar(int tamanho = TamanhoPadrao) =>
        RandomNumberGenerator.GetString(Alfabeto, tamanho);
}
