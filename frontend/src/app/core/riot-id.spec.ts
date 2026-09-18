import { parseRiotId } from './riot-id';

describe('parseRiotId', () => {
  it.each([
    'Jogador#BR1',
    'https://tracker.gg/valorant/profile/riot/Jogador%23BR1/overview',
    'tracker.gg/valorant/profile/riot/Jogador%23BR1',
  ])('aceita %s', (entrada) => {
    expect(parseRiotId(entrada)).toEqual({ nome: 'Jogador', tag: 'BR1' });
  });

  it.each(['', 'semtag', 'nome#', 'ab#BR1'])('rejeita "%s"', (entrada) => {
    expect(parseRiotId(entrada)).toBeNull();
  });
});
