namespace Encurtador.Api.Usuarios;

public class User
{
    public const int TamanhoMaximoNome = 100;
    public const int TamanhoMaximoEmail = 254;

    public int Id { get; set; }
    public required string Nome { get; set; }

    /// <summary>Sempre em minúsculas, para o e-mail ser único sem diferenciar maiúsculas.</summary>
    public required string Email { get; set; }

    /// <summary>Hash PBKDF2 gerado pelo PasswordHasher do ASP.NET Core (inclui salt e versão do algoritmo).</summary>
    public required string SenhaHash { get; set; }

    public DateTime CriadoEm { get; set; }

    /// <summary>Conta temporária criada por "entrar como visitante"; é apagada depois de 24 horas.</summary>
    public bool EhVisitante { get; set; }
}
