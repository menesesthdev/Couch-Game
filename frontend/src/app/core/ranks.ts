/** Tiers na mesma ordem e com os mesmos IDs da API (3 = Ferro 1 … 27 = Radiante). */
export const TIERS = [
  'Ferro1', 'Ferro2', 'Ferro3', 'Bronze1', 'Bronze2', 'Bronze3', 'Prata1', 'Prata2', 'Prata3',
  'Ouro1', 'Ouro2', 'Ouro3', 'Platina1', 'Platina2', 'Platina3', 'Diamante1', 'Diamante2', 'Diamante3',
  'Ascendente1', 'Ascendente2', 'Ascendente3', 'Imortal1', 'Imortal2', 'Imortal3', 'Radiante',
] as const;

export function tierId(tier: string): number {
  const i = TIERS.indexOf(tier as (typeof TIERS)[number]);
  return i < 0 ? 0 : i + 3;
}

export function nomeTier(tier: string): string {
  return tier === 'Radiante' ? 'Radiante' : tier.replace(/(\d)$/, ' $1');
}

/** Ícones oficiais servidos por valorant-api.com (mesma numeração de tier). */
export function iconeRank(tier: string): string {
  return `https://media.valorant-api.com/competitivetiers/03621f52-342b-cf4e-4f86-9350a49c6d04/${tierId(tier)}/largeicon.png`;
}

/** Primeira divisão do próximo rank — meta padrão sugerida no perfil. */
export function proximoRank(tier: string): string {
  const i = TIERS.indexOf(tier as (typeof TIERS)[number]);
  if (i < 0) return TIERS[0];
  const alvo = Math.min(TIERS.length - 1, (Math.floor(i / 3) + 1) * 3);
  return TIERS[alvo];
}
