import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { BuscaJogador } from './busca/busca-jogador';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, BuscaJogador],
  template: `
    <header class="topo">
      <a class="marca" routerLink="/" aria-label="Valorant Coach — início">
        <img src="logo.png" alt="Valorant Coach" width="62" height="44" />
      </a>

      <nav class="secoes">
        <a routerLink="/" routerLinkActive="ativo" [routerLinkActiveOptions]="{ exact: true }">Progressão</a>
        <a routerLink="/loja" routerLinkActive="ativo">Loja</a>
      </nav>

      <!-- A busca é ferramenta da área de progressão; na loja seria ruído (e aperta o header). -->
      @if (mostrarBusca()) {
        <div class="busca"><app-busca-jogador [compacta]="true" /></div>
      }
    </header>

    <router-outlet />

    <footer class="rodape">
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
      // Lockup deitado: fixa a altura e deixa a largura acompanhar a proporção.
      img { display: block; width: auto; height: 44px; }
    }
    /* Duas áreas do app: progressão é anônima, loja exige conta. */
    .secoes {
      display: flex;
      gap: 4px;
      a {
        padding: 7px 12px;
        border-radius: 999px;
        color: var(--text-muted);
        font-size: 0.875rem;
        font-weight: 500;
        text-decoration: none;
        white-space: nowrap;
        transition: background 0.15s ease, color 0.15s ease;
        &:hover { color: var(--text); background: var(--surface); }
        &.ativo { color: var(--text); background: var(--surface); }
      }
    }
    .busca {
      flex: 1;
      min-width: 0;
      max-width: 420px;
      margin-left: auto;
    }
    /* No celular a busca não cabe na mesma linha da logo e da nav: sobrava algo como 4px de
       campo, sem espaço nem para o placeholder. Abaixo de 700px o header vira duas linhas
       (logo + nav em cima, busca inteira embaixo), que é o que dá ao campo largura de verdade. */
    @media (max-width: 700px) {
      .topo {
        flex-wrap: wrap;
        align-content: center;
        height: auto;
        gap: 10px 12px;
        padding: 10px 16px;
      }
      .marca img { height: 36px; }
      .busca {
        order: 3;
        flex-basis: 100%;
        max-width: none;
        margin-left: 0;
      }
    }
    @media (max-width: 380px) {
      .topo { padding: 10px 12px; }
      .secoes a { padding: 7px 9px; }
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

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => this.router.url),
    ),
    { initialValue: this.router.url },
  );

  protected readonly mostrarBusca = computed(() => {
    const url = this.url();
    const naHome = url === '/' || url === '';
    return !naHome && !url.startsWith('/loja');
  });
}
