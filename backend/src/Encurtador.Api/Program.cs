using Encurtador.Api.Dados;
using Encurtador.Api.Links;
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

// Permite usar {codigo:codigoCurto} nas rotas (ver LinkEndpoints).
builder.Services.Configure<RouteOptions>(opcoes =>
    opcoes.ConstraintMap[CodigoCurtoConstraint.Nome] = typeof(CodigoCurtoConstraint));

var connectionString = builder.Configuration.GetConnectionString("Encurtador")
    ?? throw new InvalidOperationException("A connection string 'Encurtador' não foi configurada.");
builder.Services.AddDbContext<AppDbContext>(opcoes => opcoes.UseNpgsql(connectionString));

var app = builder.Build();

// Exceções não tratadas viram 500 e respostas de erro sem corpo (ex.: rota
// inexistente) ganham um corpo ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapLinkEndpoints();

app.Run();
