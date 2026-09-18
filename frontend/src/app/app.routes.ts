import { Routes } from '@angular/router';
import { Home } from './home/home';

export const routes: Routes = [
  { path: '', component: Home, title: 'coachgame' },
  // Mesmo formato de URL do tracker.gg: /perfil/br/nome%23tag
  { path: 'perfil/:regiao/:riotId', loadComponent: () => import('./perfil/perfil').then((m) => m.PerfilPage) },
  { path: '**', redirectTo: '' },
];
