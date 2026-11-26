namespace Encurtador.Api.Usuarios;

public record RegistrarRequest(string? Nome, string? Email, string? Senha);

public record UsuarioResponse(string Nome, string Email, bool EhVisitante)
{
    public static UsuarioResponse De(User usuario) => new(usuario.Nome, usuario.Email, usuario.EhVisitante);
}

public record LoginRequest(string? Email, string? Senha);

public record LoginResponse(string Token, DateTimeOffset ExpiraEm, UsuarioResponse Usuario);
