import { Component, input, resource } from '@angular/core';
import { toDataURL } from 'qrcode';

/** QR Code de um texto (a URL curta), com um botão para baixar a imagem. */
@Component({
  selector: 'app-qr-code',
  template: `
    @if (imagem.value(); as src) {
      <img [src]="src" width="176" height="176" [alt]="'QR Code que abre ' + texto()" />
      <a class="botao botao-secundario botao-pequeno" [href]="src" [download]="nomeArquivo()">Baixar PNG</a>
    } @else if (imagem.error()) {
      <p class="erro-campo">Não foi possível gerar o QR Code.</p>
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
      padding: 8px;
      border-radius: 8px;
      background: #ffffff;
    }
  `,
})
export class QrCode {
  readonly texto = input.required<string>();
  readonly nomeArquivo = input('qrcode.png');

  // Gerado em PNG com o dobro do tamanho exibido, para ficar nítido em telas de alta densidade.
  protected readonly imagem = resource({
    params: () => this.texto(),
    loader: ({ params }) => toDataURL(params, { margin: 1, width: 352, errorCorrectionLevel: 'M' }),
  });
}
