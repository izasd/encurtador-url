import { Routes } from '@angular/router';
import { somenteAnonimo, somenteAutenticado } from './nucleo/guardas';

// Cada página é carregada só quando é aberta, para o primeiro carregamento ficar leve.
export const routes: Routes = [
  {
    path: 'entrar',
    title: 'Entrar · Encurtador',
    canActivate: [somenteAnonimo],
    loadComponent: () => import('./paginas/entrar/entrar').then((m) => m.Entrar),
  },
  {
    path: 'cadastro',
    title: 'Criar conta · Encurtador',
    canActivate: [somenteAnonimo],
    loadComponent: () => import('./paginas/cadastro/cadastro').then((m) => m.Cadastro),
  },
  {
    path: 'links',
    title: 'Meus links · Encurtador',
    canActivate: [somenteAutenticado],
    loadComponent: () => import('./paginas/links/links').then((m) => m.Links),
  },
  {
    path: 'links/:codigo/estatisticas',
    title: 'Estatísticas · Encurtador',
    canActivate: [somenteAutenticado],
    loadComponent: () => import('./paginas/estatisticas/estatisticas').then((m) => m.Estatisticas),
  },
  { path: '', pathMatch: 'full', redirectTo: 'links' },
  { path: '**', redirectTo: 'links' },
];
