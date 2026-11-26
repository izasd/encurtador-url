using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Encurtador.Api.Usuarios;

/// <summary>Configuração da seção "Jwt", validada quando a aplicação inicia.</summary>
public class OpcoesJwt
{
    public const string Secao = "Jwt";

    /// <summary>
    /// Segredo da assinatura HMAC-SHA256: pelo menos 32 caracteres (256 bits).
    /// Em produção vem da variável de ambiente Jwt__Chave, nunca de arquivo versionado.
    /// </summary>
    [Required, MinLength(32)]
    public string Chave { get; set; } = string.Empty;

    [Required]
    public string Emissor { get; set; } = string.Empty;

    [Required]
    public string Publico { get; set; } = string.Empty;

    [Range(1, 7 * 24 * 60)]
    public int ExpiracaoEmMinutos { get; set; } = 480;

    public SymmetricSecurityKey ChaveDeAssinatura() => new(Encoding.UTF8.GetBytes(Chave));
}
