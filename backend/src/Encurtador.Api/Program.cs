using System.Text.Json;
using System.Text.Json.Serialization;
using Encurtador.Api.Cliques;
using Encurtador.Api.Dados;
using Encurtador.Api.Hospedagem;
using Encurtador.Api.Limites;
using Encurtador.Api.Links;
using Encurtador.Api.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Todos os erros (validação, 404, 409, 500...) saem no formato ProblemDetails (RFC 9457).
builder.Services.AddProblemDetails();

// Em Development o padrão é lançar exceção quando o corpo da requisição é inválido
// (JSON malformado, corpo vazio), o que o UseExceptionHandler transformaria em 500.
// Assim a resposta é 400 em qualquer ambiente, como já acontece em produção.
builder.Services.Configure<RouteHandlerOptions>(opcoes => opcoes.ThrowOnBadRequest = false);

// Enums saem como texto no JSON ("situacao": "expirado"), não como números.
builder.Services.ConfigureHttpJsonOptions(opcoes =>
    opcoes.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

// Permite usar {codigo:codigoCurto} nas rotas (ver LinkEndpoints).
builder.Services.Configure<RouteOptions>(opcoes =>
    opcoes.ConstraintMap[CodigoCurtoConstraint.Nome] = typeof(CodigoCurtoConstraint));

// Aceita também o formato de URL (postgresql://...) dos serviços de PostgreSQL gerenciado.
var connectionString = ConexaoDoBanco.Normalizar(builder.Configuration.GetConnectionString("Encurtador")
    ?? throw new InvalidOperationException("A connection string 'Encurtador' não foi configurada."));
builder.Services.AddDbContext<AppDbContext>(opcoes => opcoes.UseNpgsql(connectionString));

// O redirecionamento só enfileira o clique; o GravadorDeCliques grava no banco em segundo plano.
builder.Services.AddSingleton<FilaDeCliques>();
builder.Services.AddHostedService<GravadorDeCliques>();
builder.Services.AddSingleton<FusosHorarios>();

// Hash de senha do ASP.NET Core Identity: PBKDF2 com HMAC-SHA512, salt aleatório e 100 mil iterações.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Login com JWT (configuração na seção "Jwt").
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAutenticacaoJwt(builder.Configuration);

// Contas de visitante ("entrar como visitante") são apagadas depois de 24 horas.
builder.Services.AddHostedService<LimpezaDeVisitantes>();

// Limites de requisições por janela de tempo (ver Limites/LimitesDeRequisicao.cs).
builder.Services.AddLimitesDeRequisicao();

// Verificação de saúde para a hospedagem: a API está de pé e consegue falar com o banco.
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

// IP do visitante e HTTPS repassados pelo proxy da hospedagem (ver Hospedagem/ProxyReverso.cs).
builder.Services.AddProxyReverso(builder.Configuration);

var app = builder.Build();

// Nos contêineres, o banco é atualizado aqui, antes de a API começar a atender.
await app.AplicarMigracoesSeConfiguradoAsync();

app.UseProxyReverso();

// Exceções não tratadas viram 500 e respostas de erro sem corpo (ex.: rota
// inexistente) ganham um corpo ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Arquivos do site Angular, quando ele é publicado junto com a API (ver Hospedagem/SiteAngular.cs).
app.UseSiteAngular();

app.UseAuthentication();
app.UseAuthorization();

// Depois da autenticação, para limitar a criação de links por usuário.
app.UseRateLimiter();

app.MapAuthEndpoints();
app.MapLinkEndpoints();
app.MapEstatisticasEndpoints();

// Responde 200 ("Healthy") ou 503 ("Unhealthy"). Fica sob /api/ porque, na raiz,
// um caminho de 4 a 10 letras e números é um código de link curto.
app.MapHealthChecks("/api/saude");

app.MapSiteAngular();

await app.RunAsync();
