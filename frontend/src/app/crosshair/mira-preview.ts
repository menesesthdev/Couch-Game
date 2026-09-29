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
 * **Escala 1:1 com o jogo.** As medidas do código são pixels a 1080p, então o SVG usa uma
 * unidade por pixel: o `viewBox` tem exatamente as mesmas dimensões que os atributos `width` e
 * `height`, e nada é ampliado. Uma mira de 12 px ocupa 12 px aqui, igual ao que o jogador vê.
 *
 * Antes o quadro se ajustava ao tamanho da mira, o que ampliava as pequenas em até 4× — dava
 * para ver o desenho, mas não para julgar se a mira é discreta ou se tapa o alvo, que é o que
 * se quer saber antes de copiar o código.
 *
 * Como o quadro é fixo e maior que o palco, quem recorta é o palco: em tela estreita a janela
 * para o jogo fica menor, mas a mira continua do mesmo tamanho — é isso que mantém o 1:1.
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
      [attr.viewBox]="viewBox"
      [attr.width]="largura"
      [attr.height]="altura"
      role="img"
      [attr.aria-label]="rotulo()"
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
      flex: none;
    }
    svg {
      display: block;
      overflow: visible;
    }
  `,
})
export class MiraPreview {
  /** Janela para o jogo, em pixels de 1080p. Quem for maior que o palco é recortado por ele. */
  protected readonly largura = 190;
  protected readonly altura = 142;
  protected readonly viewBox = `${-190 / 2} ${-142 / 2} 190 142`;

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
