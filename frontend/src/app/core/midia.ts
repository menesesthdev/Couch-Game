/**
 * Imagens oficiais servidas por valorant-api.com — mesma fonte dos ícones de rank,
 * pública e sem chave. Os ids vêm da própria HenrikDev API.
 */

/** Retrato quadrado do agente, o mesmo que o tracker usa. Vem com fundo transparente. */
export function iconeAgente(id: string): string {
  return `https://media.valorant-api.com/agents/${id}/displayicon.png`;
}

/**
 * Arte do mapa já escurecida e dessaturada pela própria Riot: serve de fundo das partidas
 * sem precisar de filtro nosso, e pesa 361 KB contra 1,5 MB do `splash`. Só os mapas de
 * treino não têm essa arte, e eles nunca aparecem em competitivo.
 */
export function fundoMapa(id: string): string {
  return `https://media.valorant-api.com/maps/${id}/stylizedbackgroundimage.png`;
}
