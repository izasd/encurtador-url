import { HttpClient } from '@angular/common/http';
import { Component, DestroyRef, inject, input, resource } from '@angular/core';
import { firstValueFrom } from 'rxjs';

/** QR Code do link, gerado pela API em PNG, com um botão para baixar a imagem. */
@Component({
  selector: 'app-qr-code',
  template: `
    @if (imagem.value(); as src) {
      <img [src]="src" width="176" height="176" [alt]="'QR Code que abre ' + urlCurta()" />
      <a class="botao botao-secundario botao-pequeno" [href]="src" [download]="'qrcode-' + codigo() + '.png'">Baixar PNG</a>
    } @else if (imagem.error()) {
      <p class="erro-campo">Não foi possível carregar o QR Code.</p>
    } @else {
      <p class="dica">Gerando o QR Code…</p>
    }
  `,
  styles: `
    :host {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      gap: 1rem;
    }

    /* Sempre escuro sobre branco, mesmo no tema escuro: é o que os leitores de QR Code esperam. */
    img {
      border-radius: 8px;
      background: #ffffff;
    }
  `,
})
export class QrCode {
  readonly codigo = input.required<string>();
  readonly urlCurta = input.required<string>();

  private readonly http = inject(HttpClient);
  private urlDaImagem: string | null = null;

  // A rota da API exige o token, que um <img src="/api/..."> não enviaria. Por isso a imagem
  // é baixada pelo HttpClient (o interceptor põe o token) e exibida por uma URL de objeto (blob:).
  // Tamanho 8: a imagem sai com cerca de 300 px, nítida também em telas de alta densidade.
  protected readonly imagem = resource({
    params: () => this.codigo(),
    loader: async ({ params }) => {
      const png = await firstValueFrom(
        this.http.get(`/api/links/${encodeURIComponent(params)}/qrcode`, { params: { tamanho: 8 }, responseType: 'blob' }),
      );

      this.liberarImagem();
      this.urlDaImagem = URL.createObjectURL(png);
      return this.urlDaImagem;
    },
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => this.liberarImagem());
  }

  /** URLs de objeto ocupam memória até serem liberadas. */
  private liberarImagem(): void {
    if (this.urlDaImagem) {
      URL.revokeObjectURL(this.urlDaImagem);
      this.urlDaImagem = null;
    }
  }
}
