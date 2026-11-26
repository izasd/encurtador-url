import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { Sessao } from './nucleo/sessao';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly sessao = inject(Sessao);
  private readonly router = inject(Router);

  protected sair(): void {
    this.sessao.sair();
    void this.router.navigate(['/entrar']);
  }
}
