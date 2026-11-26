using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;

namespace Encurtador.Api.Usuarios;

public static class RegrasDeConta
{
    public const int TamanhoMinimoSenha = 8;
    public const int TamanhoMaximoSenha = 128;

    /// <summary>Devolve o nome sem espaços nas pontas, ou nulo se estiver vazio ou longo demais.</summary>
    public static string? NormalizarNome(string? nome)
    {
        var normalizado = nome?.Trim();

        return normalizado is { Length: > 0 and <= User.TamanhoMaximoNome } ? normalizado : null;
    }

    /// <summary>Devolve o e-mail aparado e em minúsculas, ou nulo se não for um endereço válido.</summary>
    public static string? NormalizarEmail(string? email)
    {
        var normalizado = email?.Trim().ToLowerInvariant();

        return normalizado is { Length: > 0 and <= User.TamanhoMaximoEmail }
            && MailAddress.TryCreate(normalizado, out var endereco)
            && endereco.Address == normalizado // recusa formatos como "Ana <ana@exemplo.com>"
                ? normalizado
                : null;
    }

    public static bool SenhaValida([NotNullWhen(true)] string? senha) =>
        senha is { Length: >= TamanhoMinimoSenha and <= TamanhoMaximoSenha };
}
