using Encurtador.Api.Dados;
using Encurtador.Api.Links;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Encurtador")
    ?? throw new InvalidOperationException("A connection string 'Encurtador' não foi configurada.");
builder.Services.AddDbContext<AppDbContext>(opcoes => opcoes.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapLinkEndpoints();

app.Run();
