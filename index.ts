const servidor = Bun.serve({
    port: 3000,
    fetch(req: Request): Response {
    return new Response("Olá, Bun + TypeScript!");
    },
});

console.log(`Servidor rodando em http://localhost:${servidor.port}`);