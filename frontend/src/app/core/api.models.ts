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

export interface Partida {
  data: string;
  mapa: string | null;
  variacaoRR: number;
  resultado: 'Vitoria' | 'Derrota' | 'Empate';
  rankApos: RankDto | null;
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

export type TipoCenario = 'TresVitorias' | 'DuasVitorias' | 'UmaVitoria' | 'Real';

export interface Cenario {
  tipo: TipoCenario;
  descricao: string;
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
