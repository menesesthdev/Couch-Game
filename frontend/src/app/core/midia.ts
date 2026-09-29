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

/**
 * Mapa usado como "parede" atrás das miras da galeria: o Icebox é o que tem uma superfície
 * grande e marcada no primeiro plano, que é o mais perto de uma parede em que se mira.
 *
 * É o mesmo para todas as miras de propósito. A galeria existe para comparar miras, e um
 * fundo diferente em cada card mudaria o contraste de uma para outra sem motivo — fora que
 * assim o navegador baixa uma imagem só.
 */
export const MAPA_PAREDE = 'e2ad5c54-4114-a870-9641-8ea21279579a';
