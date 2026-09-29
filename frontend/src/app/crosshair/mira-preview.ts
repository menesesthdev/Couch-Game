import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DesenhoMira, LinhasMira } from '../core/api.models';

/** Um retângulo do desenho, já em unidades do jogo. */
interface Peca {
  x: number;
  y: number;
  largura: number;
  altura: number;
  opacidade: number;
}

/**
 * Desenha a mira em SVG a partir do código já decodificado pelo backend.
 *
 * Trabalha nas unidades do próprio jogo e deixa o viewBox fazer a escala: assim a espessura de
 * 1 e a de 6 mantêm entre si a mesma proporção que têm no Valorant, sem nenhuma conta de pixel.
 *
 * O contorno é um traço preto com `paint-order: stroke`, ou seja, pintado antes do preenchimento.
 * Como o traço do SVG fica metade para dentro e metade para fora, a metade de dentro some sob o
 * preenchimento e sobra exatamente a espessura pedida para fora — que é como o jogo desenha.
 */
@Component({
  selector: 'app-mira-preview',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      [attr.viewBox]="viewBox()"
      role="img"
      [attr.aria-label]="rotulo()"
      preserveAspectRatio="xMidYMid meet"
    >
      @for (p of pecas(); track $index) {
        <rect
          [attr.x]="p.x"
          [attr.y]="p.y"
          [attr.width]="p.largura"
          [attr.height]="p.altura"
          [attr.fill]="desenho().cor"
          [attr.fill-opacity]="p.opacidade"
          [attr.stroke]="contorno() ? '#000' : 'none'"
          [attr.stroke-width]="contorno()?.espessura ?? 0"
          [attr.stroke-opacity]="contorno()?.opacidade ?? 0"
          paint-order="stroke"
        />
      }
    </svg>
  `,
  styles: `
    :host {
      display: block;
      width: 100%;
      aspect-ratio: 1;
    }
    svg {
      display: block;
      width: 100%;
      height: 100%;
      overflow: visible;
    }
  `,
})
export class MiraPreview {
  readonly desenho = input.required<DesenhoMira>();
  readonly rotulo = input('Prévia da mira');

  protected readonly contorno = computed(() => {
    const c = this.desenho().contorno;
    if (!c.visivel || c.espessura <= 0 || c.opacidade <= 0) return null;
    // Dobrado porque só a metade de fora do traço fica visível (ver comentário da classe).
    return { espessura: c.espessura * 2, opacidade: c.opacidade };
  });

  protected readonly pecas = computed<Peca[]>(() => {
    const d = this.desenho();
    const pecas = [...this.linhas(d.internas), ...this.linhas(d.externas)];

    if (d.ponto.aparece) {
      const lado = d.ponto.espessura;
      pecas.push({ x: -lado / 2, y: -lado / 2, largura: lado, altura: lado, opacidade: d.ponto.opacidade });
    }

    return pecas;
  });

  /**
   * O viewBox acompanha a mira, mas nunca fica menor que um mínimo: sem esse piso, uma mira de
   * um ponto só seria ampliada até encher o card e pareceria enorme ao lado de uma cruz grande.
   */
  protected readonly viewBox = computed(() => {
    const d = this.desenho();
    const alcance = Math.max(
      this.alcance(d.internas),
      this.alcance(d.externas),
      d.ponto.aparece ? d.ponto.espessura / 2 : 0,
    );
    const metade = Math.max(14, alcance + 3);
    return `${-metade} ${-metade} ${metade * 2} ${metade * 2}`;
  });

  private alcance(l: LinhasMira): number {
    return l.aparece ? l.deslocamento + Math.max(l.comprimento, l.comprimentoVertical) : 0;
  }

  /**
   * As quatro linhas de um dos conjuntos. Cima e baixo usam o comprimento vertical, que só
   * difere do horizontal quando o jogador destrava os dois no jogo.
   */
  private linhas(l: LinhasMira): Peca[] {
    if (!l.aparece) return [];

    const { comprimento: h, comprimentoVertical: v, espessura: e, deslocamento: o, opacidade } = l;
    const pecas: Peca[] = [];

    if (v > 0) {
      pecas.push(
        { x: -e / 2, y: -(o + v), largura: e, altura: v, opacidade },
        { x: -e / 2, y: o, largura: e, altura: v, opacidade },
      );
    }

    if (h > 0) {
      pecas.push(
        { x: -(o + h), y: -e / 2, largura: h, altura: e, opacidade },
        { x: o, y: -e / 2, largura: h, altura: e, opacidade },
      );
    }

    return pecas;
  }
}
