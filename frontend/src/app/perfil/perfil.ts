import { Component, computed, effect, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DecimalPipe, PercentPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CoachgameApi } from '../core/coachgame-api';
import { BuscasRecentes } from '../core/buscas-recentes';
import { REGIOES, Regiao } from '../core/api.models';
import { TIERS, iconeRank, nomeTier } from '../core/ranks';
import { TempoRelativoPipe } from '../core/tempo-relativo.pipe';
import { EstimativaPainel } from './estimativa-painel';

/** Página de perfil estilo tracker: rank, desempenho, meta e partidas recentes. */
@Component({
  selector: 'app-perfil',
  imports: [RouterLink, DecimalPipe, PercentPipe, TempoRelativoPipe, EstimativaPainel],
  templateUrl: './perfil.html',
  styleUrl: './perfil.scss',
})
export class PerfilPage {
  /** Parâmetros da rota (/perfil/:regiao/:riotId). */
  readonly regiao = input.required<string>();
  readonly riotId = input.required<string>();

  private readonly api = inject(CoachgameApi);
  private readonly recentes = inject(BuscasRecentes);

  protected readonly iconeRank = iconeRank;
  protected readonly nomeTier = nomeTier;

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

  protected readonly proximaDivisao = computed(() => {
    const p = this.dados();
    if (!p) return null;
    const i = TIERS.indexOf(p.rankAtual.tier as (typeof TIERS)[number]);
    if (i < 0 || i === TIERS.length - 1) return null;
    return { nome: nomeTier(TIERS[i + 1]), rr: p.rrParaProximaDivisao };
  });

  /** Últimos 10 resultados, do mais antigo para o mais recente (leitura da esquerda para a direita). */
  protected readonly forma = computed(() => (this.dados()?.partidas ?? []).slice(0, 10).reverse());

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
