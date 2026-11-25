using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Encurtador.Api.Cliques;
using Encurtador.Api.Links;
using Encurtador.Api.Usuarios;
using Microsoft.AspNetCore.Http;

namespace Encurtador.IntegrationTests.Infraestrutura;

/// <summary>Atalhos para os passos que quase todo teste repete: criar conta, criar link, ler respostas.</summary>
internal static class ClienteDaApi
{
    public const string Senha = "senha-dos-testes-123";

    /// <summary>As mesmas opções da API: nomes em camelCase e enums como texto.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string EmailUnico() => $"{Guid.NewGuid():N}@teste.com";

    public static string CodigoUnico() => GeradorDeCodigo.Gerar(Link.TamanhoMaximoCodigo);

    /// <summary>Cria uma conta com e-mail novo e devolve um cliente já autenticado com o token dela.</summary>
    public static async Task<HttpClient> CriarUsuarioAsync(this ApiFactory api)
    {
        var cliente = api.CriarCliente();
        var email = EmailUnico();

        var cadastro = await cliente.PostAsJsonAsync("/api/auth/registrar", new { nome = "Pessoa de Teste", email, senha = Senha });
        await LerAsync<UsuarioResponse>(cadastro, HttpStatusCode.Created);

        var login = await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha = Senha });
        var token = (await LerAsync<LoginResponse>(login, HttpStatusCode.OK)).Token;

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    public static async Task<LinkResponse> CriarLinkAsync(
        this HttpClient cliente, string urlOriginal = "https://exemplo.com/uma/pagina", string? codigo = null, string? expiraEm = null)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/links", new { urlOriginal, codigo, expiraEm });
        return await LerAsync<LinkResponse>(resposta, HttpStatusCode.Created);
    }

    /// <summary>PATCH com o tipo de conteúdo do JSON Merge Patch (RFC 7396).</summary>
    public static Task<HttpResponseMessage> AtualizarLinkAsync(this HttpClient cliente, string codigo, string json) =>
        cliente.PatchAsync($"/api/links/{codigo}", new StringContent(json, System.Text.Encoding.UTF8, "application/merge-patch+json"));

    public static async Task<EstatisticasResponse> ObterEstatisticasAsync(this HttpClient cliente, string codigo, string? fusoHorario = null)
    {
        var url = $"/api/links/{codigo}/estatisticas" + (fusoHorario is null ? "" : $"?fusoHorario={Uri.EscapeDataString(fusoHorario)}");
        return await LerAsync<EstatisticasResponse>(await cliente.GetAsync(url), HttpStatusCode.OK);
    }

    public static Task<EstatisticasResponse> AguardarCliquesAsync(this HttpClient cliente, string codigo, int total) =>
        cliente.AguardarEstatisticasAsync(codigo, e => e.TotalDeCliques >= total);

    /// <summary>
    /// Espera as estatísticas atenderem à condição: o redirecionamento só enfileira o clique,
    /// e a gravação no banco acontece em segundo plano logo depois.
    /// </summary>
    public static async Task<EstatisticasResponse> AguardarEstatisticasAsync(
        this HttpClient cliente, string codigo, Func<EstatisticasResponse, bool> condicao)
    {
        var limite = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var estatisticas = await cliente.ObterEstatisticasAsync(codigo);
            if (condicao(estatisticas) || DateTime.UtcNow > limite)
            {
                return estatisticas;
            }

            await Task.Delay(50);
        }
    }

    public static async Task<T> LerAsync<T>(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        await ConferirStatusAsync(resposta, esperado);
        return (await resposta.Content.ReadFromJsonAsync<T>(Json))!;
    }

    /// <summary>Confere o status e o formato ProblemDetails (application/problem+json) do erro.</summary>
    public static async Task<HttpValidationProblemDetails> LerProblemaAsync(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        await ConferirStatusAsync(resposta, esperado);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);

        var problema = (await resposta.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Json))!;
        Assert.Equal((int)esperado, problema.Status);
        return problema;
    }

    private static async Task ConferirStatusAsync(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        if (resposta.StatusCode != esperado)
        {
            Assert.Fail($"Esperava {(int)esperado}, mas a API respondeu {(int)resposta.StatusCode}: {await resposta.Content.ReadAsStringAsync()}");
        }
    }
}
