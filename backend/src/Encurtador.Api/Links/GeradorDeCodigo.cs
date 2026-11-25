namespace Encurtador.Api.Links;

public static class GeradorDeCodigo
{
    public const int TamanhoPadrao = 6;

    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string Gerar(int tamanho = TamanhoPadrao) =>
        new(Random.Shared.GetItems(Alfabeto.AsSpan(), tamanho));
}
