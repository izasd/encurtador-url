using System.Diagnostics.CodeAnalysis;

namespace Encurtador.Api.Links;

public static class ValidadorDeCodigo
{
    public static bool EhValido([NotNullWhen(true)] string? codigo) =>
        codigo is { Length: >= Link.TamanhoMinimoCodigo and <= Link.TamanhoMaximoCodigo }
        && codigo.All(char.IsAsciiLetterOrDigit);
}
