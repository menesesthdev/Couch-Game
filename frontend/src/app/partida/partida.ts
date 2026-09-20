import { Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ValorantCoachApi } from '../core/valorant-coach-api';
import { REGIOES, JogadorPartida, Regiao, TimePartida } from '../core/api.models';
import { iconeRank } from '../core/ranks';
import { fundoMapa, iconeAgente } from '../core/midia';

/** Scoreboard completo de uma partida, aberto a partir do histórico do perfil. */
@Component({
  selector: 'app-partida',
  imports: [RouterLink, DatePipe, DecimalPipe, PercentPipe],
  templateUrl: './partida.html',
  styleUrl: './partida.scss',
})
export class PartidaPage {
  /** Parâmetros da rota (/partida/:regiao/:matchId). */
  readonly regiao = input.required<string>();
  readonly matchId = input.required<string>();
  /** Riot ID de quem abriu a partida, vindo do perfil: destaca a linha dele e alimenta o "voltar". */
  readonly jogador = input<string>();

  private readonly api = inject(ValorantCoachApi);

  protected readonly iconeRank = iconeRank;
  protected readonly iconeAgente = iconeAgente;
  protected readonly fundoMapa = fundoMapa;

  protected readonly regiaoApi = computed(() => {
    const r = this.regiao().toLowerCase();
    return (REGIOES.find((x) => x.valor.toLowerCase() === r)?.valor ?? 'Br') as Regiao;
  });

  protected readonly partida = rxResource({
    params: () => ({ regiao: this.regiaoApi(), matchId: this.matchId() }),
    stream: ({ params }) => this.api.partida(params.regiao, params.matchId),
  });

  protected readonly dados = computed(() => (this.partida.hasValue() ? this.partida.value() : undefined));

  /** A linha do jogador que abriu a página, se ela estiver neste scoreboard. */
  protected readonly euJogador = computed<JogadorPartida | undefined>(() => {
    const riotId = this.jogador()?.toLowerCase();
    if (!riotId) return undefined;
    return this.dados()
      ?.times.flatMap((t) => t.jogadores)
      .find((j) => j.riotId.toLowerCase() === riotId);
  });

  /** Tudo na página é contado do ponto de vista deste time — azul quando não sabemos quem é o jogador. */
  protected readonly meuTime = computed<TimePartida>(() => this.euJogador()?.time ?? 'Azul');

  /** Sem saber de quem é o perfil, a partida vira neutra: nada de "seu time" nem "vitória". */
  protected readonly temJogador = computed(() => !!this.euJogador());

  /** Meu time primeiro, como no tracker. */
  protected readonly times = computed(() => {
    const times = this.dados()?.times ?? [];
    return [...times].sort((a, b) => (a.time === this.meuTime() ? -1 : b.time === this.meuTime() ? 1 : 0));
  });

  protected readonly resultado = computed(() => this.times()[0]?.resultado);

  /** Frase do placar: com dono, "Vitória 13 – 7"; sem dono, "Azul 13 – 7 Vermelho". */
  protected readonly frasePlacar = computed(() => {
    const [meu, outro] = this.times();
    if (!meu || !outro) return '';
    return this.temJogador()
      ? `${this.textoResultado(meu.resultado)} ${meu.rounds} – ${outro.rounds}`
      : `Azul ${meu.rounds} – ${outro.rounds} Vermelho`;
  });

  /** Volta para o perfil de quem abriu a partida; sem esse dado, cai na home. */
  protected readonly linkVoltar = computed(() =>
    this.jogador() ? ['/perfil', this.regiao(), encodeURIComponent(this.jogador()!)] : ['/'],
  );

  protected textoResultado(resultado: string | undefined): string {
    return resultado === 'Vitoria' ? 'Vitória' : resultado === 'Derrota' ? 'Derrota' : 'Empate';
  }

  protected nomeTime(time: TimePartida): string {
    if (!this.temJogador()) return time === 'Azul' ? 'Time azul' : 'Time vermelho';
    return time === this.meuTime() ? 'Seu time' : 'Adversários';
  }

  protected mensagemErro(): string {
    const erro = this.partida.error();
    return erro instanceof Error ? erro.message : 'Não foi possível carregar esta partida.';
  }
}
