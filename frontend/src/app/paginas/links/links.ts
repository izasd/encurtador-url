import { DatePipe } from '@angular/common';
import { HttpClient, HttpErrorResponse, httpResource } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { QrCode } from '../../componentes/qr-code/qr-code';
import { Link } from '../../nucleo/api';
import { lerErros } from '../../nucleo/erros';
import { Sessao } from '../../nucleo/sessao';
import { enderecoDoLinkCurto, urlLegivel } from '../../nucleo/url';

type Campo = 'urlOriginal' | 'codigo';

/** Data no formato do campo de data (aaaa-mm-dd), no fuso de quem está usando. */
function dataLocal(data: Date): string {
  const doisDigitos = (n: number) => String(n).padStart(2, '0');
  return `${data.getFullYear()}-${doisDigitos(data.getMonth() + 1)}-${doisDigitos(data.getDate())}`;
}

/** Mesma regra da API (ValidadorDeCodigo.cs): vazio ou de 4 a 10 letras e números. */
function codigoValido(controle: AbstractControl<string>): ValidationErrors | null {
  const codigo = controle.value.trim();
  return codigo === '' || /^[A-Za-z0-9]{4,10}$/.test(codigo) ? null : { codigo: true };
}

@Component({
  selector: 'app-links',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, QrCode],
  templateUrl: './links.html',
  styleUrl: './links.css',
})
export class Links {
  protected readonly sessao = inject(Sessao);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  protected readonly links = httpResource<Link[]>(() => '/api/links', { defaultValue: [] });

  protected readonly itens = computed(() =>
    this.links.value().map((link) => ({
      ...link,
      urlLegivel: urlLegivel(link.urlOriginal),
      endereco: enderecoDoLinkCurto(link.urlCurta, link.codigo),
    })),
  );

  // A URL só precisa começar com http:// ou https://: espaços e acentos são aceitos,
  // porque a API normaliza a URL (um espaço vira %20).
  protected readonly formulario = inject(NonNullableFormBuilder).group({
    urlOriginal: ['', [Validators.required, Validators.pattern(/^\s*https?:\/\/\S/i), Validators.maxLength(2048)]],
    codigo: ['', codigoValido],
  });

  protected readonly enviando = signal(false);
  protected readonly erroGeral = signal<string | null>(null);
  protected readonly criado = signal<Link | null>(null);
  protected readonly copiado = signal<string | null>(null);
  protected readonly falhaAoCopiar = signal(false);
  protected readonly qrAberto = signal<string | null>(null);
  private readonly errosDaApi = signal<Partial<Record<Campo, string>>>({});
  private temporizadorDaCopia?: ReturnType<typeof setTimeout>;

  // Gerenciamento de um link (painel "Gerenciar"): só um painel aberto por vez.
  protected readonly gerenciando = signal<string | null>(null);
  protected readonly confirmandoExclusao = signal<string | null>(null);
  protected readonly salvando = signal<string | null>(null);
  protected readonly erroDaAcao = signal<{ codigo: string; mensagem: string } | null>(null);
  protected readonly hoje = dataLocal(new Date());
  protected readonly dataLocal = (iso: string | null) => (iso ? dataLocal(new Date(iso)) : '');

  protected async criar(): Promise<void> {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const { urlOriginal, codigo } = this.formulario.getRawValue();
    this.enviando.set(true);
    this.erroGeral.set(null);
    this.errosDaApi.set({});

    try {
      const link = await firstValueFrom(
        this.http.post<Link>('/api/links', { urlOriginal: urlOriginal.trim(), codigo: codigo.trim() || null }),
      );

      this.criado.set(link);
      this.formulario.reset();
      this.links.reload();
    } catch (erro) {
      const erros = lerErros(erro, 'Não foi possível encurtar o link.');

      // Código personalizado já usado (409): a mensagem aparece junto do campo do código.
      if (erro instanceof HttpErrorResponse && erro.status === 409 && erros.geral) {
        this.errosDaApi.set({ codigo: erros.geral });
      } else {
        this.errosDaApi.set(erros.campos);
        this.erroGeral.set(erros.geral);
      }
    } finally {
      this.enviando.set(false);
    }
  }

  protected async copiar(link: Link): Promise<void> {
    try {
      await navigator.clipboard.writeText(link.urlCurta);
      this.falhaAoCopiar.set(false);
      this.copiado.set(link.codigo);
      clearTimeout(this.temporizadorDaCopia);
      this.temporizadorDaCopia = setTimeout(() => this.copiado.set(null), 2000);
    } catch {
      this.copiado.set(null);
      this.falhaAoCopiar.set(true);
    }
  }

  protected alternarQr(codigo: string): void {
    this.qrAberto.update((aberto) => (aberto === codigo ? null : codigo));
  }

  protected alternarGerenciar(codigo: string): void {
    this.gerenciando.update((aberto) => (aberto === codigo ? null : codigo));
    this.confirmandoExclusao.set(null);
    this.erroDaAcao.set(null);
  }

  protected async alterarAtivo(link: Link, ativo: boolean): Promise<void> {
    await this.atualizar(link, { ativo });
  }

  /** A data escolhida vale até o fim daquele dia, no fuso de quem está usando. */
  protected async salvarExpiracao(link: Link, data: string): Promise<void> {
    const expiraEm = new Date(`${data}T23:59:59`);
    if (!data || Number.isNaN(expiraEm.getTime()) || expiraEm <= new Date()) {
      this.erroDaAcao.set({ codigo: link.codigo, mensagem: 'Escolha hoje ou uma data futura.' });
      return;
    }

    await this.atualizar(link, { expiraEm: expiraEm.toISOString() });
  }

  protected async removerExpiracao(link: Link): Promise<void> {
    await this.atualizar(link, { expiraEm: null });
  }

  protected async excluir(link: Link): Promise<void> {
    await this.executarAcao(link, 'Não foi possível excluir o link.', async () => {
      await firstValueFrom(this.http.delete(`/api/links/${encodeURIComponent(link.codigo)}`));
      this.gerenciando.set(null);
      if (this.criado()?.codigo === link.codigo) {
        this.criado.set(null);
      }
    });
  }

  /** Envia só o que muda (JSON Merge Patch): "expiraEm": null remove a expiração. */
  private async atualizar(link: Link, mudancas: { ativo?: boolean; expiraEm?: string | null }): Promise<void> {
    await this.executarAcao(link, 'Não foi possível salvar a alteração.', async () => {
      await firstValueFrom(this.http.patch<Link>(`/api/links/${encodeURIComponent(link.codigo)}`, mudancas));
    });
  }

  private async executarAcao(link: Link, mensagemPadrao: string, acao: () => Promise<void>): Promise<void> {
    this.salvando.set(link.codigo);
    this.erroDaAcao.set(null);

    try {
      await acao();
      this.confirmandoExclusao.set(null);
      this.links.reload();
    } catch (erro) {
      const erros = lerErros(erro, mensagemPadrao);
      this.erroDaAcao.set({ codigo: link.codigo, mensagem: erros.campos['expiraEm'] ?? erros.geral ?? mensagemPadrao });
    } finally {
      this.salvando.set(null);
    }
  }

  /** Mensagem de erro do campo: primeiro a da API, depois a da validação local. */
  protected mensagem(campo: Campo): string | null {
    const daApi = this.errosDaApi()[campo];
    if (daApi) {
      return daApi;
    }

    const controle = this.formulario.controls[campo];
    if (!controle.touched || controle.valid) {
      return null;
    }

    if (campo === 'codigo') {
      return 'Use de 4 a 10 letras ou números, sem espaços nem símbolos.';
    }

    return controle.hasError('required')
      ? 'Informe a URL que você quer encurtar.'
      : 'Informe uma URL começando com http:// ou https://, com até 2048 caracteres.';
  }

  /** Visitante que quer uma conta de verdade: sai da conta temporária e vai para o cadastro. */
  protected criarConta(): void {
    this.sessao.sair();
    void this.router.navigate(['/cadastro']);
  }
}
