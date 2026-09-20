import { Routes } from '@angular/router';
import { Home } from './home/home';

export const routes: Routes = [
  { path: '', component: Home, title: 'Valorant Coach' },
  // Mesmo formato de URL do tracker.gg: /perfil/br/nome%23tag
  { path: 'perfil/:regiao/:riotId', loadComponent: () => import('./perfil/perfil').then((m) => m.PerfilPage) },
  // Aberta a partir de uma linha do histórico; ?jogador=nome#tag destaca quem abriu.
  { path: 'partida/:regiao/:matchId', loadComponent: () => import('./partida/partida').then((m) => m.PartidaPage) },
  // Única área que depende de conta conectada — separada do resto de propósito.
  { path: 'loja', loadComponent: () => import('./loja/loja').then((m) => m.LojaPage), title: 'Loja · Valorant Coach' },
  { path: '**', redirectTo: '' },
];
