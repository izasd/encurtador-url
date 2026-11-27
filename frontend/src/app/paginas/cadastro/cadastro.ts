import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Apresentacao } from '../../componentes/apresentacao/apresentacao';
import { lerErros } from '../../nucleo/erros';
import { Sessao } from '../../nucleo/sessao';

type Campo = 'nome' | 'email' | 'senha';

const OBRIGATORIO: Record<Campo, string> = {
  nome: 'Informe seu nome.',
  email: 'Informe seu e-mail.',
  senha: 'Crie uma senha.',
};

@Component({
  selector: 'app-cadastro',
  imports: [ReactiveFormsModule, RouterLink, Apresentacao],
  templateUrl: './cadastro.html',
})
export class Cadastro {
  private readonly sessao = inject(Sessao);
  private readonly router = inject(Router);

  // Mesmas regras da API (RegrasDeConta.cs), para avisar antes de enviar.
  protected readonly formulario = inject(NonNullableFormBuilder).group({
    nome: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
  });

  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);
  private readonly errosDaApi = signal<Partial<Record<Campo, string>>>({});

  protected async cadastrar(): Promise<void> {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const { nome, email, senha } = this.formulario.getRawValue();
    this.enviando.set(true);
    this.erro.set(null);
    this.errosDaApi.set({});

    try {
      await this.sessao.cadastrar(nome, email, senha);
      await this.router.navigateByUrl('/links');
    } catch (erro) {
      const erros = lerErros(erro, 'Não foi possível criar a conta.');

      // E-mail já cadastrado (409): a mensagem aparece junto do campo de e-mail.
      if (erro instanceof HttpErrorResponse && erro.status === 409 && erros.geral) {
        this.errosDaApi.set({ email: erros.geral });
      } else {
        this.errosDaApi.set(erros.campos);
        this.erro.set(erros.geral);
      }
    } finally {
      this.enviando.set(false);
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

    if (controle.hasError('required')) {
      return OBRIGATORIO[campo];
    }

    if (controle.hasError('email')) {
      return 'Informe um e-mail válido.';
    }

    if (controle.hasError('minlength')) {
      return 'A senha precisa de pelo menos 8 caracteres.';
    }

    return campo === 'nome' ? 'Use até 100 caracteres.' : 'Use até 128 caracteres.';
  }
}
