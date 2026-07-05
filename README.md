# Encurtador de URL

Projeto simples de encurtador de URLs feito com Bun, TypeScript e SQLite.

## Requisitos

- Bun instalado

## Instalação

Instale as dependências do projeto:

```bash
bun install
```

## Execução

Inicie o servidor em modo de desenvolvimento:

```bash
bun --hot index.ts
```

O servidor ficará disponível em:

```text
http://localhost:3000
```

## Rotas

- `GET /` - abre a interface web
- `POST /api/encurtar` - cria uma URL encurtada
- `GET /api/urls` - lista as URLs cadastradas
- `GET /:codigo` - redireciona para a URL original
