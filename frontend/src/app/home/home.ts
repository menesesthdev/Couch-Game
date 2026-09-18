import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BuscaJogador } from '../busca/busca-jogador';
import { BuscasRecentes } from '../core/buscas-recentes';
import { iconeRank } from '../core/ranks';
import { segmentoPerfil } from '../core/riot-id';

@Component({
  selector: 'app-home',
  imports: [BuscaJogador, RouterLink],
  template: `
    <section class="hero">
      <h1>Quanto falta para o seu próximo rank?</h1>
      <p class="sub">Descubra em quantas partidas e dias você chega na sua meta no Valorant, com base no seu desempenho real.</p>

      <app-busca-jogador />
      <p class="dica">Digite seu Riot ID (ex.: <strong>nome#BR1</strong>) ou cole o link do seu perfil no tracker.gg.</p>

      @if (recentes.itens().length) {
        <div class="recentes">
          <span class="rotulo-recentes">Buscados recentemente</span>
          <div class="lista">
            @for (r of recentes.itens(); track r.nome + r.tag) {
              <a class="recente" [routerLink]="['/perfil', r.regiao.toLowerCase(), segmento(r)]">
                @if (r.tier) {
                  <img [src]="iconeRank(r.tier)" alt="" />
                }
                {{ r.nome }}<span>#{{ r.tag }}</span>
              </a>
            }
          </div>
        </div>
      }
    </section>

    <ol class="passos">
      <li>
        <span class="numero">1</span>
        <strong>Busque seu perfil</strong>
        <span>Usamos seu histórico público de partidas ranqueadas.</span>
      </li>
      <li>
        <span class="numero">2</span>
        <strong>Escolha a meta</strong>
        <span>O rank que você quer e até quando.</span>
      </li>
      <li>
        <span class="numero">3</span>
        <strong>Veja a estimativa</strong>
        <span>Partidas, dias e o que precisa mudar para chegar no prazo.</span>
      </li>
    </ol>
  `,
  styles: `
    :host {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 72px;
      padding: 48px 16px 64px;
    }
    .hero {
      width: 100%;
      max-width: 680px;
      text-align: center;
    }
    h1 {
      margin: 0 0 14px;
      font-size: clamp(1.875rem, 5vw, 2.75rem);
      font-weight: 600;
      letter-spacing: -0.035em;
      line-height: 1.1;
    }
    .sub {
      margin: 0 auto 36px;
      max-width: 520px;
      font-size: 1.0625rem;
      color: var(--text-muted);
    }
    .dica {
      margin: 14px 0 0;
      font-size: 0.8125rem;
      color: var(--text-faint);
      strong { color: var(--text-muted); font-weight: 500; }
    }
    .recentes { margin-top: 36px; }
    .rotulo-recentes {
      display: block;
      margin-bottom: 12px;
      font-size: 0.8125rem;
      color: var(--text-faint);
    }
    .lista {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: 8px;
    }
    .recente {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      height: 38px;
      padding: 0 14px 0 10px;
      border-radius: 999px;
      background: var(--surface);
      color: var(--text);
      font-size: 0.875rem;
      font-weight: 500;
      text-decoration: none;
      transition: background 0.15s ease;
      &:hover { background: var(--surface-strong); }
      img { width: 22px; height: 22px; object-fit: contain; }
      span { color: var(--text-faint); font-weight: 400; margin-left: -6px; }
    }
    .passos {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 16px;
      width: 100%;
      max-width: 880px;
      margin: 0;
      padding: 0;
      list-style: none;
      @media (max-width: 720px) { grid-template-columns: 1fr; }
      li {
        display: flex;
        flex-direction: column;
        gap: 4px;
        padding: 20px 22px;
        border-radius: var(--radius-md);
        background: var(--card);
        box-shadow: var(--card-shadow);
      }
      .numero {
        display: grid;
        place-items: center;
        width: 28px;
        height: 28px;
        margin-bottom: 10px;
        border-radius: 50%;
        background: var(--surface-strong);
        font-size: 0.8125rem;
        font-weight: 600;
      }
      strong { font-weight: 600; }
      span:last-child { font-size: 0.875rem; color: var(--text-muted); }
    }
  `,
})
export class Home {
  protected readonly recentes = inject(BuscasRecentes);
  protected readonly iconeRank = iconeRank;
  protected readonly segmento = segmentoPerfil;
}
