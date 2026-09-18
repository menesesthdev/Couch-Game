export type Regiao = 'Br' | 'Latam' | 'Na' | 'Eu' | 'Ap' | 'Kr';

export type TipoCenario = 'TresVitorias' | 'DuasVitorias' | 'UmaVitoria' | 'Real';

export interface RankOpcao {
  tier: string;
  nome: string;
  rr: number;
}

export interface EstimativaRequest {
  perfil: string;
  regiao: Regiao;
  rankAlvo: string;
  dataLimite: string; // yyyy-MM-dd
}

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

export interface EstimativaResponse {
  riotId: string;
  rankAtual: RankOpcao;
  rankAlvo: RankOpcao;
  dataLimite: string;
  rrFaltando: number;
  desempenho: {
    partidasAnalisadas: number;
    winRate: number;
    rrMedioGanho: number;
    rrMedioPerdido: number;
    partidasPorDia: number;
  };
  usouMediasPadrao: boolean;
  cenarios: Cenario[];
  aviso: string;
}
