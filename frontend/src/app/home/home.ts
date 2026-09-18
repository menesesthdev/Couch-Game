import { Component } from '@angular/core';
import { BuscaJogador } from '../busca/busca-jogador';

@Component({
  selector: 'app-home',
  imports: [BuscaJogador],
  template: `
    <section class="hero">
      <h1>Quanto falta para o seu próximo rank?</h1>
      <p>Busque um jogador de Valorant para ver o desempenho recente e estimar em quantas partidas e dias ele chega na meta.</p>
      <app-busca-jogador />
      <ul class="dicas">
        <li><span>Riot ID</span> nome#tag</li>
        <li><span>Link</span> tracker.gg/valorant/profile/riot/…</li>
      </ul>
    </section>
  `,
  styles: `
    :host {
      flex: 1;
      display: grid;
      place-items: center;
      padding: 0 16px 14vh;
    }
    .hero {
      width: 100%;
      max-width: 680px;
      text-align: center;
    }
    h1 {
      margin: 0 0 12px;
      font-size: clamp(1.75rem, 5vw, 2.5rem);
      font-weight: 600;
      letter-spacing: -0.03em;
      line-height: 1.15;
    }
    p {
      margin: 0 auto 32px;
      max-width: 520px;
      color: var(--text-muted);
    }
    .dicas {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: 8px 20px;
      margin: 20px 0 0;
      padding: 0;
      list-style: none;
      font-size: 0.8125rem;
      color: var(--text-faint);
      span { color: var(--text-muted); font-weight: 500; margin-right: 4px; }
    }
  `,
})
export class Home {}
