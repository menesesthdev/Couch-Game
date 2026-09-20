import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { Subject, catchError, of, switchMap, tap } from 'rxjs';
import { ValorantCoachApi } from '../core/valorant-coach-api';
import { Estimativa, Perfil } from '../core/api.models';
import { TIERS, iconeRank, nomeTier, proximoRank } from '../core/ranks';

/** yyyy-MM-dd no fuso do navegador. */
function dataLocal(data: Date): string {
  const d = new Date(data.getTime() - data.getTimezoneOffset() * 60_000);
  return d.toISOString().slice(0, 10);
}

function daquiA(dias: number): string {
  const data = new Date();
  data.setDate(data.getDate() + dias);
  return dataLocal(data);
}

/** Dias entre duas datas yyyy-MM-dd. */
function diasEntre(de: string, ate: string): number {
  return Math.round((Date.parse(ate) - Date.parse(de)) / 86_400_000);
}

type ModoPrazo = 'ato' | 'data';

/**
 * Meta + resposta. A meta é uma frase editável ("Quero chegar em X até o final do ato") que
 * recalcula sozinha; a resposta vem primeiro em linguagem direta, com uma linha do tempo de hoje
 * até o prazo mostrando onde o jogador chega.
 */
@Component({
  selector: 'app-estimativa-painel',
  imports: [DatePipe, DecimalPipe, PercentPipe],
  templateUrl: './estimativa-painel.html',
  styleUrl: './estimativa-painel.scss',
})
export class EstimativaPainel {
  readonly perfil = input.required<Perfil>();

  private readonly api = inject(ValorantCoachApi);
  private readonly pedidos = new Subject<void>();

  protected readonly iconeRank = iconeRank;
  protected readonly hoje = daquiA(0);
  protected readonly amanha = daquiA(1);

  /** Ato em andamento: o prazo padrão é o último dia dele (no fuso do navegador). */
  protected readonly ato = rxResource({ stream: () => this.api.atoAtual() });
  protected readonly fimDoAto = computed(() => {
    const ato = this.ato.hasValue() ? this.ato.value() : null;
    if (!ato) return null;
    const fim = dataLocal(new Date(Date.parse(ato.fim) - 1));
    // Ato terminando hoje não serve de prazo (a meta precisa de pelo menos um dia).
    return fim >= this.amanha ? fim : null;
  });

  protected readonly modoPrazo = signal<ModoPrazo>('ato');
  protected readonly dataEscolhida = signal(daquiA(30));
  protected readonly dataLimite = computed(() =>
    this.modoPrazo() === 'ato' && this.fimDoAto() ? this.fimDoAto()! : this.dataEscolhida(),
  );
  protected readonly nomePrazo = computed(() => (this.modoPrazo() === 'ato' && this.fimDoAto() ? 'fim do ato' : 'prazo'));

  protected readonly rankAlvo = signal('');
  protected readonly carregando = signal(false);
  protected readonly erro = signal('');
  protected readonly resultado = signal<Estimativa | null>(null);

  /** Só ranks acima do atual fazem sentido como meta. */
  protected readonly opcoesAlvo = computed(() => {
    const atual = TIERS.indexOf(this.perfil().rankAtual.tier as (typeof TIERS)[number]);
    return TIERS.slice(atual + 1).map((t) => ({ tier: t, nome: nomeTier(t) }));
  });

  protected readonly real = computed(() => this.resultado()?.cenarios.find((c) => c.tipo === 'Real'));
  protected readonly simulacoes = computed(() => this.resultado()?.cenarios.filter((c) => c.tipo === 'Simulacao') ?? []);

  /** Folga (positiva) ou atraso (negativo) em dias em relação ao prazo, no ritmo real. */
  protected readonly folga = computed(() => {
    const r = this.resultado();
    const c = this.real();
    return r && c?.dataEstimada ? diasEntre(c.dataEstimada, r.dataLimite) : null;
  });

  /**
   * Linha do tempo de hoje até o prazo. A escala cobre o que for maior entre o prazo e a chegada
   * estimada, para que uma chegada atrasada apareça depois da marca do prazo.
   */
  protected readonly linhaDoTempo = computed(() => {
    const r = this.resultado();
    const c = this.real();
    if (!r || !c?.alcancavel || c.diasEstimados === null || r.rrFaltando === 0) return null;
    const diasPrazo = Math.max(1, diasEntre(this.hoje, r.dataLimite));
    const total = Math.max(diasPrazo, c.diasEstimados);
    const prazo = (diasPrazo / total) * 100;
    const chegada = Math.max(2, (c.diasEstimados / total) * 100);
    // Perto das bordas o rótulo alinha pelo lado de dentro, para não sair do card.
    const alinhar = (pct: number) => (pct < 18 ? 'inicio' : pct > 82 ? 'fim' : 'meio');
    return { prazo, chegada, atrasado: c.diasEstimados > diasPrazo, alinharPrazo: alinhar(prazo), alinharChegada: alinhar(chegada) };
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

    // Ao abrir (ou trocar) um perfil — e assim que o calendário responder — calcula com a meta
    // padrão: primeira divisão do próximo rank até o final do ato.
    effect(() => {
      const perfil = this.perfil();
      if (this.ato.isLoading()) return;
      const temAto = !!this.fimDoAto();
      untracked(() => {
        this.resultado.set(null);
        if (perfil.rankAtual.tier === 'Radiante') return;
        this.modoPrazo.set(temAto ? 'ato' : 'data');
        this.rankAlvo.set(proximoRank(perfil.rankAtual.tier));
        this.pedidos.next();
      });
    });
  }

  protected mudarRank(tier: string): void {
    this.rankAlvo.set(tier);
    this.pedidos.next();
  }

  protected usarFimDoAto(): void {
    if (this.modoPrazo() === 'ato') return;
    this.modoPrazo.set('ato');
    this.pedidos.next();
  }

  protected usarOutraData(): void {
    if (this.modoPrazo() === 'data') return;
    this.modoPrazo.set('data');
    this.pedidos.next();
  }

  protected mudarData(data: string): void {
    if (!data || data < this.amanha) return;
    this.dataEscolhida.set(data);
    this.pedidos.next();
  }

  protected diasAte(data: string): number {
    return diasEntre(this.hoje, data);
  }

  /** "7 de cada 10" para múltiplos de 10%; senão, só o percentual. */
  protected emPartidas(winRate: number): string | null {
    const pct = Math.round(winRate * 100);
    return pct % 10 === 0 ? `${pct / 10} de cada 10 partidas` : null;
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
