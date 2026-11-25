# syntax=docker/dockerfile:1

# Imagem da aplicação: a API ASP.NET Core, que também serve o site Angular em /app/.
# Para subir junto com o PostgreSQL, use o docker-compose.yml.

# 1. Build do site Angular
FROM node:22-alpine AS frontend
ENV NG_CLI_ANALYTICS=false
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npx ng build

# 2. Build da API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/src/Encurtador.Api/Encurtador.Api.csproj Encurtador.Api/
RUN dotnet restore Encurtador.Api/Encurtador.Api.csproj
COPY backend/src/Encurtador.Api/ Encurtador.Api/
RUN dotnet publish Encurtador.Api/Encurtador.Api.csproj --no-restore -c Release -o /publicacao -p:UseAppHost=false

# 3. Imagem final, só com o runtime do ASP.NET Core. A variante "chiseled" não tem shell nem
#    gerenciador de pacotes (menos coisa para atacar); a "extra" mantém ICU e fusos horários.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra
WORKDIR /app

# A imagem não traz a biblioteca do Kerberos, que o PostgreSQL desta aplicação não usa. Sem isto,
# o Npgsql tenta carregá-la ao abrir a primeira conexão e escreve um erro (inofensivo) no log.
ENV PGGSSENCMODE=disable
COPY --from=backend /publicacao ./
COPY --from=frontend /src/frontend/dist/frontend/browser ./wwwroot/app/

# Usuário sem privilégios, que a imagem oficial já traz. A API escuta na porta 8080.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Encurtador.Api.dll"]
