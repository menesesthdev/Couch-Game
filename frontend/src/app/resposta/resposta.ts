import { Component, computed, input } from '@angular/core';
import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { Cenario, EstimativaResponse } from '../estimativa/estimativa.models';

/** Resposta da estimativa, em texto corrido como uma mensagem do assistente — sem gráficos. */
@Component({
  selector: 'app-resposta',
  imports: [DatePipe, DecimalPipe, PercentPipe],
  templateUrl: './resposta.html',
  styleUrl: './resposta.scss',
})
export class Resposta {
  readonly estimativa = input.required<EstimativaResponse>();

  protected readonly real = computed(() => this.estimativa().cenarios.find((c) => c.tipo === 'Real'));
  protected readonly fixos = computed(() => this.estimativa().cenarios.filter((c) => c.tipo !== 'Real'));
  protected readonly jaAtingiu = computed(() => this.estimativa().rrFaltando === 0);

  protected status(c: Cenario): string {
    if (!c.alcancavel) return 'não sobe nesse ritmo';
    return c.dentroDoPrazo ? 'dentro do prazo' : 'depois do prazo';
  }
}
