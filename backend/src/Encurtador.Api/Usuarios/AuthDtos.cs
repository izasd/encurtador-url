namespace Encurtador.Api.Usuarios;

public record RegistrarRequest(string? Nome, string? Email, string? Senha);

public record UsuarioResponse(string Nome, string Email);
