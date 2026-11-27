import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Apresentacao } from '../../componentes/apresentacao/apresentacao';
import { lerErros } from '../../nucleo/erros';
import { Sessao } from '../../nucleo/sessao';
import { destinoSeguro } from '../../nucleo/url';

/** De onde veio a tentativa de entrar: o erro aparece junto do botão que a pessoa usou. */
type Origem = 'formulario' | 'visitante';

@Component({
  selector: 'app-entrar',
  imports: [ReactiveFormsModule, RouterLink, Apresentacao],
  templateUrl: './entrar.html',
})
export class Entrar {
  private readonly sessao = inject(Sessao);
  private readonly router = inject(Router);
  private readonly parametros = inject(ActivatedRoute).snapshot.queryParamMap;

  protected readonly formulario = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', Validators.required],
  });

  protected readonly enviando = signal(false);
  private readonly erro = signal<{ origem: Origem; mensagem: string } | null>(null);
  protected readonly sessaoExpirada = this.parametros.get('sessao') === 'expirada';

  protected async entrar(): Promise<void> {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const { email, senha } = this.formulario.getRawValue();
    await this.executar(() => this.sessao.entrar(email, senha), 'Não foi possível entrar.', 'formulario');
  }

  protected async entrarComoVisitante(): Promise<void> {
    await this.executar(
      () => this.sessao.entrarComoVisitante(),
      'Não foi possível criar a conta de visitante.',
      'visitante',
    );
  }

  protected invalido(campo: 'email' | 'senha'): boolean {
    const controle = this.formulario.controls[campo];
    return controle.invalid && controle.touched;
  }

  protected erroDe(origem: Origem): string | null {
    const erro = this.erro();
    return erro?.origem === origem ? erro.mensagem : null;
  }

  private async executar(acao: () => Promise<void>, mensagemPadrao: string, origem: Origem): Promise<void> {
    this.enviando.set(true);
    this.erro.set(null);

    try {
      await acao();
      await this.router.navigateByUrl(destinoSeguro(this.parametros.get('voltar')));
    } catch (erro) {
      this.erro.set({ origem, mensagem: lerErros(erro, mensagemPadrao).geral ?? mensagemPadrao });
    } finally {
      this.enviando.set(false);
    }
  }
}
