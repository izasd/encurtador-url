import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Link } from '../../nucleo/api';
import { Sessao } from '../../nucleo/sessao';
import { urlLegivel } from '../../nucleo/url';

@Component({
  selector: 'app-links',
  imports: [DatePipe],
  templateUrl: './links.html',
  styleUrl: './links.css',
})
export class Links {
  protected readonly sessao = inject(Sessao);
  private readonly router = inject(Router);

  protected readonly links = httpResource<Link[]>(() => '/api/links', { defaultValue: [] });

  protected readonly itens = computed(() =>
    this.links.value().map((link) => ({ ...link, urlLegivel: urlLegivel(link.urlOriginal) })),
  );

  /** Visitante que quer uma conta de verdade: sai da conta temporária e vai para o cadastro. */
  protected criarConta(): void {
    this.sessao.sair();
    void this.router.navigate(['/cadastro']);
  }
}
