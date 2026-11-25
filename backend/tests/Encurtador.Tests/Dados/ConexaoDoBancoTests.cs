using Encurtador.Api.Dados;
using Npgsql;

namespace Encurtador.Tests.Dados;

public class ConexaoDoBancoTests
{
    [Fact]
    public void Normalizar_FormatoDoNpgsql_DevolveSemMudar()
    {
        const string original = "Host=localhost;Port=5433;Database=encurtador;Username=encurtador;Password=a:b@c";

        Assert.Equal(original, ConexaoDoBanco.Normalizar(original));
    }

    [Fact]
    public void Normalizar_Url_ConverteParaOFormatoDoNpgsql()
    {
        var resultado = ConexaoDoBanco.Normalizar(
            "postgresql://ana:s%40nha%3Aforte@ep-exemplo-123.us-east-1.aws.neon.tech:6543/encurtador?sslmode=require&channel_binding=require");

        var conexao = new NpgsqlConnectionStringBuilder(resultado);
        Assert.Equal("ep-exemplo-123.us-east-1.aws.neon.tech", conexao.Host);
        Assert.Equal(6543, conexao.Port);
        Assert.Equal("encurtador", conexao.Database);
        Assert.Equal("ana", conexao.Username);
        Assert.Equal("s@nha:forte", conexao.Password);
        Assert.Equal(SslMode.Require, conexao.SslMode);
        Assert.Equal(ChannelBinding.Require, conexao.ChannelBinding);
    }

    [Fact]
    public void Normalizar_UrlSemPortaNemParametros_UsaOsPadroesDoPostgreSql()
    {
        var conexao = new NpgsqlConnectionStringBuilder(ConexaoDoBanco.Normalizar("postgres://ana:senha@db.exemplo.com/encurtador"));

        Assert.Equal("db.exemplo.com", conexao.Host);
        Assert.Equal(5432, conexao.Port);
        Assert.Equal(SslMode.Prefer, conexao.SslMode);
    }

    [Theory]
    [InlineData("disable", SslMode.Disable)]
    [InlineData("require", SslMode.Require)]
    [InlineData("verify-ca", SslMode.VerifyCA)]
    [InlineData("verify-full", SslMode.VerifyFull)]
    public void Normalizar_ModosDeSslDoPostgreSql_ViramOsDoNpgsql(string modo, SslMode esperado)
    {
        var conexao = new NpgsqlConnectionStringBuilder(ConexaoDoBanco.Normalizar($"postgresql://ana:senha@db/encurtador?sslmode={modo}"));

        Assert.Equal(esperado, conexao.SslMode);
    }

    [Fact]
    public void Normalizar_ParametroQueONpgsqlNaoConhece_LancaExcecao()
    {
        Assert.Throws<ArgumentException>(() => ConexaoDoBanco.Normalizar("postgresql://ana:senha@db/encurtador?opcao_inventada=1"));
    }
}
