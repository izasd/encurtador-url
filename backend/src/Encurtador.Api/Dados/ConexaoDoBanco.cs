using Microsoft.AspNetCore.WebUtilities;
using Npgsql;

namespace Encurtador.Api.Dados;

/// <summary>
/// Aceita a connection string no formato do Npgsql (Host=...;Database=...) ou como URL
/// (postgresql://usuario:senha@host:5432/banco?sslmode=require), que é o formato que serviços
/// de PostgreSQL gerenciado, como Neon e Render, mostram para copiar.
/// </summary>
public static class ConexaoDoBanco
{
    public static string Normalizar(string connectionString)
    {
        var texto = connectionString.Trim();
        if (!texto.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            && !texto.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var url = new Uri(texto);
        var credenciais = url.UserInfo.Split(':', 2);

        var conexao = new NpgsqlConnectionStringBuilder
        {
            Host = url.DnsSafeHost,
            Port = url.IsDefaultPort ? NpgsqlConnection.DefaultPort : url.Port,
            Database = Uri.UnescapeDataString(url.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credenciais[0]),
            Password = credenciais.Length > 1 ? Uri.UnescapeDataString(credenciais[1]) : null,
        };

        foreach (var (parametro, valor) in QueryHelpers.ParseQuery(url.Query))
        {
            switch (parametro.ToLowerInvariant())
            {
                case "sslmode":
                    conexao.SslMode = valor.ToString().ToLowerInvariant() switch
                    {
                        "verify-ca" => SslMode.VerifyCA,
                        "verify-full" => SslMode.VerifyFull,
                        var modo => Enum.Parse<SslMode>(modo, ignoreCase: true),
                    };
                    break;
                case "channel_binding":
                    conexao.ChannelBinding = Enum.Parse<ChannelBinding>(valor.ToString(), ignoreCase: true);
                    break;
                default:
                    // Outras opções seguem com o mesmo nome; uma que o Npgsql não conheça impede a
                    // API de iniciar, com uma mensagem que diz qual é.
                    conexao[parametro] = valor.ToString();
                    break;
            }
        }

        return conexao.ConnectionString;
    }
}
