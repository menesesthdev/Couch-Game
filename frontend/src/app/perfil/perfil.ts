import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ValorantCoachApi } from '../core/valorant-coach-api';
import { BuscasRecentes } from '../core/buscas-recentes';
import { EstatisticasRecentes, PartidaResumo, Periodo, REGIOES, Regiao } from '../core/api.models';
import { TIERS, iconeRank, nomeTier } from '../core/ranks';
import { fundoMapa, iconeAgente } from '../core/midia';
import { TempoRelativoPipe } from '../core/tempo-relativo.pipe';
import { EstimativaPainel } from './estimativa-painel';

/** Página de perfil estilo tracker: rank, desempenho, meta e partidas recentes. */
@Component({
  selector: 'app-perfil',
  imports: [RouterLink, DatePipe, DecimalPipe, PercentPipe, TempoRelativoPipe, EstimativaPainel],
  templateUrl: './perfil.html',
  styleUrl: './perfil.scss',
})
export class PerfilPage {
  /** Parâmetros da rota (/perfil/:regiao/:riotId). */
  readonly regiao = input.required<string>();
  readonly riotId = input.required<string>();

  private readonly api = inject(ValorantCoachApi);
  private readonly recentes = inject(BuscasRecentes);

  protected readonly iconeRank = iconeRank;
  protected readonly nomeTier = nomeTier;
  protected readonly iconeAgente = iconeAgente;
  protected readonly fundoMapa = fundoMapa;

  protected readonly regiaoApi = computed(() => {
    const r = this.regiao().toLowerCase();
    return (REGIOES.find((x) => x.valor.toLowerCase() === r)?.valor ?? 'Br') as Regiao;
  });
  protected readonly regiaoNome = computed(() => REGIOES.find((x) => x.valor === this.regiaoApi())!.nome);

  protected readonly perfil = rxResource({
    params: () => ({ riotId: decodeURIComponent(this.riotId()), regiao: this.regiaoApi() }),
    stream: ({ params }) => this.api.perfil(params.riotId, params.regiao),
  });

  protected readonly dados = computed(() => (this.perfil.hasValue() ? this.perfil.value() : undefined));

  /** Últimas partidas por padrão; o jogador pode trocar para o ato inteiro. */
  protected readonly periodo = signal<Periodo>('Ultimas');

  /** Recurso separado do perfil: é outra chamada à fonte e a página não depende dela para abrir. */
  protected readonly desempenho = rxResource({
    params: () => ({
      riotId: decodeURIComponent(this.riotId()),
      regiao: this.regiaoApi(),
      periodo: this.periodo(),
    }),
    stream: ({ params }) => this.api.desempenho(params.riotId, params.regiao, params.periodo),
  });

  protected readonly analise = computed(() => (this.desempenho.hasValue() ? this.desempenho.value() : undefined));

  protected readonly stats = computed(() => {
    const e = this.analise()?.estatisticas;
    return e && e.partidasAnalisadas > 0 ? e : undefined;
  });

  protected readonly partidas = computed<PartidaResumo[]>(() => this.analise()?.partidas ?? []);

  protected trocarPeriodo(valor: string) {
    this.periodo.set(valor as Periodo);
  }

  protected readonly proximaDivisao = computed(() => {
    const p = this.dados();
    if (!p) return null;
    const i = TIERS.indexOf(p.rankAtual.tier as (typeof TIERS)[number]);
    if (i < 0 || i === TIERS.length - 1) return null;
    return { nome: nomeTier(TIERS[i + 1]), rr: p.rrParaProximaDivisao };
  });

  protected readonly limitePartidas = 10;
  protected readonly todasPartidas = signal(false);
  protected readonly partidasVisiveis = computed(() => {
    const todas = this.partidas();
    return this.todasPartidas() ? todas : todas.slice(0, this.limitePartidas);
  });

  /** Últimos 10 resultados, do mais antigo para o mais recente (leitura da esquerda para a direita). */
  protected readonly forma = computed(() => this.partidas().slice(0, 10).reverse());

  // ---- Frases em linguagem direta: o número sozinho não diz nada para quem não é do meio. ----

  /** O que o KD significa na prática. */
  protected fraseKd(e: EstatisticasRecentes): string {
    if (e.kd >= 1.3) return 'Você elimina bem mais do que morre';
    if (e.kd >= 1.1) return 'Você elimina mais do que morre';
    if (e.kd >= 0.95) return 'Você troca em pé de igualdade';
    if (e.kd >= 0.8) return 'Você morre um pouco mais do que elimina';
    return 'Você morre bem mais do que elimina';
  }

  /** "1 a cada 5 tiros certeiros" — mais concreto que uma porcentagem solta. */
  protected fraseHeadshot(taxa: number): string {
    if (taxa <= 0) return 'Nenhum tiro na cabeça no período';
    return `Cerca de 1 a cada ${Math.round(1 / taxa)} tiros que acertam`;
  }

  /** 100 de dano é uma vida cheia: a régua que todo jogador já tem na cabeça. */
  protected fraseDano(dano: number): string {
    const vidas = dano / 100;
    if (vidas < 0.8) return 'Menos de uma vida por round';
    if (vidas < 1.2) return 'Cerca de uma vida por round';
    return `Cerca de ${vidas.toLocaleString('pt-BR', { maximumFractionDigits: 1 })} vidas por round`;
  }

  /** Win rate como fração intuitiva: "vence 1 em cada 2". */
  protected fraseVitorias(e: EstatisticasRecentes): string {
    if (e.winRate <= 0) return 'Nenhuma vitória no período';
    if (e.winRate >= 1) return 'Venceu todas';
    return `Vence cerca de 1 a cada ${Math.round(1 / e.winRate)} partidas`;
  }

  protected placar(p: PartidaResumo): string {
    return `${p.roundsGanhos} – ${p.roundsPerdidos}`;
  }

  protected mensagemErroDesempenho(): string {
    const erro = this.desempenho.error();
    return erro instanceof Error ? erro.message : 'Não foi possível carregar suas partidas.';
  }

  constructor() {
    // Mantém o rank atualizado nas buscas recentes do autocomplete.
    effect(() => {
      const p = this.dados();
      if (p) this.recentes.registrar({ nome: p.nome, tag: p.tag, regiao: p.regiao, tier: p.rankAtual.tier });
    });
  }

  protected mensagemErro(): string {
    const erro = this.perfil.error();
    return erro instanceof Error ? erro.message : 'Não foi possível carregar este perfil.';
  }
}
