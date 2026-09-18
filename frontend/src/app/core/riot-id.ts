export interface RiotIdParseado {
  nome: string;
  tag: string;
}

/** Aceita `nome#tag` ou link do tracker.gg. Mesmas regras do RiotId do backend. */
export function parseRiotId(entrada: string): RiotIdParseado | null {
  let texto = entrada.trim();
  const link = /tracker\.gg\/valorant\/profile\/riot\/([^/?#]+)/i.exec(texto);
  if (link) texto = link[1];

  try {
    texto = decodeURIComponent(texto);
  } catch {
    return null;
  }

  const i = texto.lastIndexOf('#');
  if (i <= 0 || i === texto.length - 1) return null;
  const nome = texto.slice(0, i).trim();
  const tag = texto.slice(i + 1).trim();
  if (nome.length < 3 || nome.length > 16 || tag.length < 3 || tag.length > 5) return null;
  return { nome, tag };
}

/** Segmento de URL no estilo do tracker.gg: `nome%23tag`. */
export function segmentoPerfil({ nome, tag }: RiotIdParseado): string {
  return encodeURIComponent(`${nome}#${tag}`);
}
