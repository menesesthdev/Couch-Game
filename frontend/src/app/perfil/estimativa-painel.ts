import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CoachgameApi } from '../core/coachgame-api';
import { Cenario, Estimativa, Perfil } from '../core/api.models';
import { TIERS, iconeRank, nomeTier, proximoRank } from '../core/ranks';

function daquiA(dias: number): string {
  const data = new Date();
  data.setDate(data.getDate() + dias);
  return data.toISOString().slice(0, 10);
}

/** Meta (rank-alvo + prazo) e o resultado detalhado da estimativa. */
@Component({
  selector: 'app-estimativa-painel',
  imports: [FormsModule, DatePipe, DecimalPipe, PercentPipe],
  templateUrl: './estimativa-painel.html',
  styleUrl: './estimativa-painel.scss',
})
export class EstimativaPainel {
  readonly perfil = input.required<Perfil>();

  private readonly api = inject(CoachgameApi);

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

  constructor() {
    // Ao abrir (ou trocar) um perfil, já calcula com a meta padrão: primeira divisão do próximo rank.
    effect(() => {
      const perfil = this.perfil();
      untracked(() => {
        this.resultado.set(null);
        if (perfil.rankAtual.tier === 'Radiante') return;
        this.rankAlvo.set(proximoRank(perfil.rankAtual.tier));
        this.calcular();
      });
    });
  }

  protected calcular(): void {
    if (!this.rankAlvo() || !this.dataLimite() || this.carregando()) return;
    this.carregando.set(true);
    this.erro.set('');
    this.api
      .estimar({
        perfil: this.perfil().riotId,
        regiao: this.perfil().regiao,
        rankAlvo: this.rankAlvo(),
        dataLimite: this.dataLimite(),
      })
      .subscribe({
        next: (r) => {
          this.resultado.set(r);
          this.carregando.set(false);
        },
        error: (e: Error) => {
          this.erro.set(e.message);
          this.carregando.set(false);
        },
      });
  }

  protected status(c: Cenario): { texto: string; classe: string } {
    if (!c.alcancavel) return { texto: 'Não sobe nesse ritmo', classe: 'ruim' };
    return c.dentroDoPrazo ? { texto: 'Dentro do prazo', classe: 'bom' } : { texto: 'Depois do prazo', classe: 'neutro' };
  }

  protected dias(n: number | null): string {
    return n === 1 ? '1 dia' : `${n} dias`;
  }
}
