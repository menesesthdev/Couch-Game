import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { BuscaJogador } from './busca/busca-jogador';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, BuscaJogador],
  template: `
    <header class="topo">
      <a class="marca" routerLink="/" aria-label="coachgame — início">
        <img src="logo.png" alt="coachgame" width="40" height="40" />
      </a>
      @if (!naHome()) {
        <div class="busca"><app-busca-jogador [compacta]="true" /></div>
      }
    </header>

    <router-outlet />

    <footer class="rodape">
      Dados públicos via HenrikDev API. O coachgame não é afiliado à Riot Games.
    </footer>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      min-height: 100dvh;
    }
    .topo {
      position: sticky;
      top: 0;
      z-index: 20;
      display: flex;
      align-items: center;
      gap: 24px;
      height: 64px;
      padding: 0 20px;
      background: color-mix(in srgb, var(--bg) 88%, transparent);
      backdrop-filter: blur(12px);
    }
    .marca {
      display: flex;
      flex-shrink: 0;
      transition: opacity 0.15s ease;
      &:hover { opacity: 0.85; }
      img { display: block; width: 40px; height: 40px; }
    }
    .busca {
      flex: 1;
      min-width: 0;
      max-width: 420px;
      margin-left: auto;
    }
    @media (max-width: 520px) {
      .topo { gap: 12px; padding: 0 12px; }
    }
    .rodape {
      padding: 24px 16px 28px;
      text-align: center;
      font-size: 0.75rem;
      color: var(--text-faint);
    }
  `,
})
export class App {
  private readonly router = inject(Router);

  protected readonly naHome = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => this.router.url === '/' || this.router.url === ''),
    ),
    { initialValue: true },
  );
}
