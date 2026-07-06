import {
    inserirUrl, buscarPorCodigo, registrarAcesso, listarUrls
} from "./banco";
import { gerarCodigo, urlValida } from "./util";
import type { RespostaErro } from "./tipos";

function json(dados: unknown, status: number = 200): Response {
    return new Response(JSON.stringify(dados, null, 2), {
        status,
        headers: { "Content-Type": "application/json; charset=utf-8" },
    });
}

function erro(mensagem: string, status: number): Response {
    const resp: RespostaErro = { erro: mensagem };
    return json(resp, status);
}

const servidor = Bun.serve({
    // Desafio E: porta lida do arquivo .env pela variavel PORTA.
    port: Number(Bun.env.PORTA ?? 3000),

    async fetch(req: Request): Promise<Response> {
        const url = new URL(req.url);
        const caminho = url.pathname;
        const metodo = req.method;

    // POST /api/encurtar -> cria nova URL curta
    if (metodo === "POST" && caminho === "/api/encurtar") {
        // Desafio A: aceita codigo personalizado opcional no corpo da requisicao.
        const corpo = await req.json() as { urlOriginal?: string; codigo?: string };
        if (!corpo.urlOriginal || !urlValida(corpo.urlOriginal)) {
        return erro("URL inválida. Forneça http:// ou https://", 400);
        }
        // Desafio A: valida formato do codigo personalizado.
        if (corpo.codigo && !/^[A-Za-z0-9]{4,10}$/.test(corpo.codigo)) {
        return erro("Codigo deve ter apenas letras e numeros, de 4 a 10 caracteres", 400);
        }

        const codigo = corpo.codigo ?? gerarCodigo();
        // Desafio A: retorna 409 Conflict se o codigo ja existir.
        if (corpo.codigo && buscarPorCodigo(codigo)) {
        return erro("Codigo ja existe", 409);
        }

        const registro = inserirUrl(codigo, corpo.urlOriginal);
        return json(registro, 201);
    }

    // GET /api/urls -> lista todas
    if (metodo === "GET" && caminho === "/api/urls") {
        return json(listarUrls());
    }

    // GET /:codigo -> redireciona
    if (metodo === "GET" && /^\/[A-Za-z0-9]{4,10}$/.test(caminho)) {
        const codigo = caminho.slice(1);
        const registro = buscarPorCodigo(codigo);
        if (!registro) return erro("Código não encontrado", 404);
        registrarAcesso(codigo);
        return Response.redirect(registro.urlOriginal, 302);
    }

    // GET / -> serve o frontend
    if (metodo === "GET" && caminho === "/") {
        return new Response(Bun.file("./public/index.html"));
    }

    if (metodo === "GET" && caminho === "/app.js") {
        return new Response(Bun.file("./public/app.js"));
    }

    return erro("Rota não encontrada", 404);
    },
});

console.log(`■ Servidor pronto em http://localhost:${servidor.port}`);
