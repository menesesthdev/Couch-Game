export type Regiao = 'Br' | 'Latam' | 'Na' | 'Eu' | 'Ap' | 'Kr';

export const REGIOES: { valor: Regiao; nome: string }[] = [
  { valor: 'Br', nome: 'BR' },
  { valor: 'Latam', nome: 'LATAM' },
  { valor: 'Na', nome: 'NA' },
  { valor: 'Eu', nome: 'EU' },
  { valor: 'Ap', nome: 'AP' },
  { valor: 'Kr', nome: 'KR' },
];

export interface RankDto {
  tier: string;
  nome: string;
  rr: number;
}

export interface SugestaoJogador {
  riotId: string;
  nome: string;
  tag: string;
  regiao: Regiao;
  rank: RankDto;
}

export interface Desempenho {
  partidasAnalisadas: number;
  vitorias: number;
  derrotas: number;
  saldoRR: number;
  winRate: number;
  rrMedioGanho: number;
  rrMedioPerdido: number;
  partidasPorDia: number;
}

export type Resultado = 'Vitoria' | 'Derrota' | 'Empate';

export interface MapaDto {
  id: string;
  nome: string;
}

export interface AgenteDto {
  id: string;
  nome: string;
}

export interface Partida {
  /** Abre o scoreboard completo em /partida/:regiao/:matchId. Ausente em partidas antigas da fonte. */
  matchId: string | null;
  data: string;
  mapa: MapaDto | null;
  variacaoRR: number;
  resultado: Resultado;
  rankApos: RankDto | null;
}

export type Periodo = 'Ultimas' | 'AtoAtual';

/** Resumo do período analisado — alimenta o card "Seu jogo". */
export interface EstatisticasRecentes {
  partidasAnalisadas: number;
  vitorias: number;
  derrotas: number;
  empates: number;
  winRate: number;
  kd: number;
  taxaHeadshot: number;
  danoPorRound: number;
  pontuacaoPorRound: number;
  abatesPorPartida: number;
  mortesPorPartida: number;
  sequencia: { tipo: Resultado; quantidade: number } | null;
  topAgentes: { agente: AgenteDto; partidas: number; vitorias: number; winRate: number; kd: number }[];
}

export interface EstatisticasPartida {
  abates: number;
  mortes: number;
  assistencias: number;
  kd: number;
  taxaHeadshot: number;
  danoFeito: number;
  danoRecebido: number;
  pontuacao: number;
  danoPorRound: number;
  pontuacaoPorRound: number;
}

/** Linha da lista de partidas: desempenho e RR já casados pelo backend. */
export interface PartidaResumo {
  matchId: string;
  data: string;
  mapa: MapaDto | null;
  agente: AgenteDto | null;
  resultado: Resultado;
  roundsGanhos: number;
  roundsPerdidos: number;
  /** Ausente quando a partida não tem entrada no histórico de RR (colocação, por exemplo). */
  variacaoRR: number | null;
  rankApos: RankDto | null;
  estatisticas: EstatisticasPartida;
}

export interface DesempenhoAnalisado {
  periodo: Periodo;
  /** Só vem preenchido no período do ato. */
  ato: { nome: string; inicio: string; fim: string } | null;
  estatisticas: EstatisticasRecentes;
  partidas: PartidaResumo[];
}

export type TimePartida = 'Azul' | 'Vermelho';

export interface JogadorPartida {
  puuid: string;
  riotId: string;
  nome: string;
  tag: string;
  anonimo: boolean;
  agente: AgenteDto | null;
  time: TimePartida;
  rank: RankDto;
  nivel: number;
  estatisticas: EstatisticasPartida;
}

export interface TimeDetalhe {
  time: TimePartida;
  rounds: number;
  resultado: Resultado;
  jogadores: JogadorPartida[];
}

export interface DetalhePartida {
  matchId: string;
  data: string;
  mapa: MapaDto | null;
  modo: string | null;
  duracaoEmMinutos: number;
  totalRounds: number;
  times: TimeDetalhe[];
  rounds: { numero: number; vencedor: TimePartida; desfecho: string }[];
}

export interface Perfil {
  riotId: string;
  nome: string;
  tag: string;
  regiao: Regiao;
  rankAtual: RankDto;
  progressoDivisao: number;
  rrParaProximaDivisao: number;
  desempenho: Desempenho;
  partidas: Partida[];
  atualizadoEm: string;
}

export type TipoCenario = 'Real' | 'Simulacao';

export interface Cenario {
  tipo: TipoCenario;
  descricao: string;
  winRate: number;
  partidasPorDia: number;
  rrMedioPorPartida: number;
  alcancavel: boolean;
  partidasNecessarias: number | null;
  diasEstimados: number | null;
  dataEstimada: string | null;
  dentroDoPrazo: boolean;
}

export interface Estimativa {
  perfil: Perfil;
  rankAlvo: RankDto;
  dataLimite: string;
  rrFaltando: number;
  degraus: { rank: RankDto; rrFaltando: number }[];
  usouMediasPadrao: boolean;
  cenarios: Cenario[];
  requisitos: {
    diasAtePrazo: number;
    partidasNecessarias: number | null;
    partidasPorDiaNecessarias: number | null;
    winRateNecessario: number | null;
  };
  aviso: string;
}

export interface EstimativaRequest {
  perfil: string;
  regiao: Regiao;
  rankAlvo: string;
  dataLimite: string; // yyyy-MM-dd
}

export interface AtoCompetitivo {
  nome: string;
  inicio: string;
  fim: string;
}

// ---- Loja (única parte do app que depende de conta conectada) ----

export interface ConexaoLoja {
  conectado: boolean;
  expiraEm: string | null;
}

export interface ItemLoja {
  id: string;
  nome: string;
  imagem: string | null;
  preco: number;
}

export interface OfertaDesconto {
  item: ItemLoja;
  precoOriginal: number;
  precoComDesconto: number;
  descontoPercentual: number;
}

export interface BundleLoja {
  id: string;
  nome: string;
  imagem: string | null;
  preco: number;
  precoBase: number | null;
  restanteEmSegundos: number;
  itens: ItemLoja[];
}

export interface MinhaLoja {
  diaria: ItemLoja[];
  restanteDiariaEmSegundos: number;
  totalDiaria: number;
  bundles: BundleLoja[];
  mercadoNoturno: OfertaDesconto[];
  restanteMercadoNoturnoEmSegundos: number | null;
  carteira: { valorantPoints: number; radianite: number; kingdom: number };
}
