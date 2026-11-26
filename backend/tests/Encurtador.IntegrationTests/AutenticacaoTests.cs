using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Encurtador.Api.Links;
using Encurtador.Api.Usuarios;
using Encurtador.IntegrationTests.Infraestrutura;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Encurtador.IntegrationTests;

[Collection(ColecaoDaApi.Nome)]
public class AutenticacaoTests(ApiFactory api)
{
    [Fact]
    public async Task Registrar_ELogin_DevolvemUmTokenAceitoNasRotasProtegidas()
    {
        var cliente = api.CriarCliente();
        var email = ClienteDaApi.EmailUnico();

        var cadastro = await cliente.PostAsJsonAsync("/api/auth/registrar",
            new { nome = "  Ana Souza ", email = $" {email.ToUpperInvariant()} ", senha = ClienteDaApi.Senha });
        var usuario = await ClienteDaApi.LerAsync<UsuarioResponse>(cadastro, HttpStatusCode.Created);
        Assert.Equal(new UsuarioResponse("Ana Souza", email, EhVisitante: false), usuario);

        var login = await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha = ClienteDaApi.Senha });
        var sessao = await ClienteDaApi.LerAsync<LoginResponse>(login, HttpStatusCode.OK);
        Assert.Equal(usuario, sessao.Usuario);
        Assert.True(sessao.ExpiraEm > DateTimeOffset.UtcNow);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessao.Token);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/links")).StatusCode);
    }

    [Fact]
    public async Task Registrar_EmailJaCadastrado_Retorna409()
    {
        var cliente = api.CriarCliente();
        var email = ClienteDaApi.EmailUnico();

        await cliente.PostAsJsonAsync("/api/auth/registrar", new { nome = "Ana", email, senha = ClienteDaApi.Senha });
        var repetido = await cliente.PostAsJsonAsync("/api/auth/registrar",
            new { nome = "Outra Ana", email = email.ToUpperInvariant(), senha = ClienteDaApi.Senha });

        var problema = await ClienteDaApi.LerProblemaAsync(repetido, HttpStatusCode.Conflict);
        Assert.Equal("Já existe uma conta com este e-mail.", problema.Detail);
    }

    [Fact]
    public async Task Registrar_DadosInvalidos_Retorna400ComOErroDeCadaCampo()
    {
        var resposta = await api.CriarCliente().PostAsJsonAsync("/api/auth/registrar",
            new { nome = " ", email = "nao-e-email", senha = "curta" });

        var problema = await ClienteDaApi.LerProblemaAsync(resposta, HttpStatusCode.BadRequest);
        Assert.Equal(["email", "nome", "senha"], problema.Errors.Keys.Order());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Login_SenhaErradaOuEmailSemConta_Retorna401ComAMesmaMensagem(bool emailExiste)
    {
        var cliente = api.CriarCliente();
        var email = ClienteDaApi.EmailUnico();
        if (emailExiste)
        {
            await cliente.PostAsJsonAsync("/api/auth/registrar", new { nome = "Ana", email, senha = ClienteDaApi.Senha });
        }

        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha = "outra-senha-qualquer" });

        var problema = await ClienteDaApi.LerProblemaAsync(resposta, HttpStatusCode.Unauthorized);
        Assert.Equal("E-mail ou senha incorretos.", problema.Detail);
    }

    [Fact]
    public async Task RotaProtegida_SemToken_Retorna401()
    {
        var resposta = await api.CriarCliente().GetAsync("/api/links");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task RotaProtegida_TokenAssinadoComOutraChave_Retorna401()
    {
        // Mesmo emissor, público e formato da API: só a assinatura não confere.
        var tokenForjado = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "encurtador-api",
            Audience = "encurtador",
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = "1" },
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("uma-chave-que-nao-e-a-da-api-mas-tem-32-caracteres")),
                SecurityAlgorithms.HmacSha256),
        });

        var cliente = api.CriarCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenForjado);

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/links")).StatusCode);
    }

    [Fact]
    public async Task Visitante_RecebeUmaContaTemporariaComLinksDeExemplo()
    {
        var cliente = api.CriarCliente();

        var sessao = await ClienteDaApi.LerAsync<LoginResponse>(
            await cliente.PostAsync("/api/auth/visitante", content: null), HttpStatusCode.OK);
        Assert.True(sessao.Usuario.EhVisitante);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessao.Token);
        var links = await ClienteDaApi.LerAsync<List<LinkResponse>>(await cliente.GetAsync("/api/links"), HttpStatusCode.OK);

        Assert.Equal(4, links.Count);
        Assert.Contains(links, l => l.Situacao == SituacaoDoLink.Desativado);
        Assert.Contains(links, l => l is { Situacao: SituacaoDoLink.Disponivel, ExpiraEm: not null });
        Assert.True((await cliente.ObterEstatisticasAsync(links[0].Codigo)).TotalDeCliques > 0);
    }
}
