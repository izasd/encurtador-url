using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Encurtador.Api.Dados;

public static class DbUpdateExceptionExtensions
{
    public static bool EhViolacaoDeUnicidade(this DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
