import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { Subject, catchError, of, switchMap, tap } from 'rxjs';
import { CoachgameApi } from '../core/coachgame-api';
import { Cenario, Estimativa, Perfil, TipoCenario } from '../core/api.models';
import { TIERS, iconeRank, nomeTier, proximoRank } from '../core/ranks';

function daquiA(dias: number): string {
  const data = new Date();
  data.setDate(data.getDate() + dias);
  return data.toISOString().slice(0, 10);
}

function diasEntre(de: string, ate: string): number {
  return Math.round((new Date(ate).getTime() - new Date(de).getTime()) / 86_400_000);
}

const TITULOS: Record<Exclude<TipoCenario, 'Real'>, string> = {
  TresVitorias: 'Vencendo as 3',
  DuasVitorias: 'Vencendo 2 de 3',
  UmaVitoria: 'Vencendo 1 de 3',
};

/**
 * Meta + resposta. A meta é uma frase editável ("Quero chegar em X até Y") que recalcula
 * sozinha a cada mudança; a resposta vem primeiro em linguagem direta, os detalhes depois.
 */
@Component({
  selector: 'app-estimativa-painel',
  imports: [DatePipe, DecimalPipe, PercentPipe],
  templateUrl: './estimativa-painel.html',
  styleUrl: './estimativa-painel.scss',
})
export class EstimativaPainel {
  readonly perfil = input.required<Perfil>();

  private readonly api = inject(CoachgameApi);
  private readonly pedidos = new Subject<void>();

  protected readonly iconeRank = iconeRank;
  protected readonly amanha = daquiA(1);

  protected readonly rankAlvo = signal('');
  protected readonly dataLimite = signal(daquiA(30));
  protected readonly carregando = signal(false);
  protected readonly erro = signal('');
  protected readonly resultado = signal<Estimativa | null>(null);

  /** Só ranks acima do atual fazem sentido como meta. */
  protected readonly opcoesAlvo = computed(() => {
    const atual = TIERS.indexOf(this.perfil().rankAtual.tier as (typeof TIERS)[number]);
    return TIERS.slice(atual + 1).map((t) => ({ tier: t, nome: nomeTier(t) }));
  });

  protected readonly real = computed(() => this.resultado()?.cenarios.find((c) => c.tipo === 'Real'));
  protected readonly fixos = computed(() => this.resultado()?.cenarios.filter((c) => c.tipo !== 'Real') ?? []);

  /** Folga (positiva) ou atraso (negativo) em dias em relação ao prazo, no ritmo real. */
  protected readonly folga = computed(() => {
    const r = this.resultado();
    const c = this.real();
    return r && c?.dataEstimada ? diasEntre(c.dataEstimada, r.dataLimite) : null;
  });

  constructor() {
    // Cancela o cálculo anterior se o usuário mudar a meta antes da resposta chegar.
    this.pedidos
      .pipe(
        tap(() => {
          this.carregando.set(true);
          this.erro.set('');
        }),
        switchMap(() =>
          this.api
            .estimar({
              perfil: this.perfil().riotId,
              regiao: this.perfil().regiao,
              rankAlvo: this.rankAlvo(),
              dataLimite: this.dataLimite(),
            })
            .pipe(
              catchError((e: Error) => {
                this.erro.set(e.message);
                return of(null);
              }),
            ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((r) => {
        if (r) this.resultado.set(r);
        this.carregando.set(false);
      });

    // Ao abrir (ou trocar) um perfil, já calcula com a meta padrão: primeira divisão do próximo rank.
    effect(() => {
      const perfil = this.perfil();
      untracked(() => {
        this.resultado.set(null);
        if (perfil.rankAtual.tier === 'Radiante') return;
        this.rankAlvo.set(proximoRank(perfil.rankAtual.tier));
        this.pedidos.next();
      });
    });
  }

  protected mudarRank(tier: string): void {
    this.rankAlvo.set(tier);
    this.pedidos.next();
  }

  protected mudarData(data: string): void {
    if (!data || data < this.amanha) return;
    this.dataLimite.set(data);
    this.pedidos.next();
  }

  protected titulo(c: Cenario): string {
    return TITULOS[c.tipo as Exclude<TipoCenario, 'Real'>] ?? c.descricao;
  }

  protected status(c: Cenario): { texto: string; classe: string } {
    if (!c.alcancavel) return { texto: 'Não sobe', classe: 'ruim' };
    return c.dentroDoPrazo ? { texto: 'Chega no prazo', classe: 'bom' } : { texto: 'Passa do prazo', classe: 'neutro' };
  }

  protected dias(n: number | null | undefined): string {
    return n === 1 ? '1 dia' : `${n ?? 0} dias`;
  }

  protected partidas(n: number | null | undefined): string {
    return n === 1 ? '1 partida' : `${n ?? 0} partidas`;
  }

  /** Ritmo legível: abaixo de 1 partida/dia vira "1 partida a cada N dias". */
  protected ritmo(porDia: number): { valor: string; unidade: string } {
    if (porDia >= 1) {
      return { valor: porDia.toLocaleString('pt-BR', { maximumFractionDigits: 1 }), unidade: 'partidas por dia' };
    }
    const cada = Math.max(2, Math.round(1 / porDia));
    return { valor: '1 partida', unidade: `a cada ${cada} dias` };
  }

  protected abs(n: number): number {
    return Math.abs(n);
  }
}
