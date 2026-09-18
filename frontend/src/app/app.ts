import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { catchError, of } from 'rxjs';
import { Composer } from './composer/composer';
import { Resposta } from './resposta/resposta';
import { EstimativaService } from './estimativa/estimativa.service';
import { EstimativaRequest, EstimativaResponse } from './estimativa/estimativa.models';

interface Troca {
  id: number;
  pedido: EstimativaRequest;
  rankAlvoNome: string;
  resposta?: EstimativaResponse;
  erro?: string;
}

@Component({
  selector: 'app-root',
  imports: [Composer, Resposta, DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly service = inject(EstimativaService);
  private readonly fim = viewChild<ElementRef<HTMLElement>>('fim');
  private proximoId = 0;

  protected readonly ranks = toSignal(this.service.ranks().pipe(catchError(() => of([]))), { initialValue: [] });
  protected readonly trocas = signal<Troca[]>([]);
  protected readonly carregando = signal(false);

  protected estimar(pedido: EstimativaRequest): void {
    const id = ++this.proximoId;
    const rankAlvoNome = this.ranks().find((r) => r.tier === pedido.rankAlvo)?.nome ?? pedido.rankAlvo;
    this.trocas.update((t) => [...t, { id, pedido, rankAlvoNome }]);
    this.carregando.set(true);
    this.rolarParaFim();

    this.service.estimar(pedido).subscribe({
      next: (resposta) => this.concluir(id, { resposta }),
      error: (e: Error) => this.concluir(id, { erro: e.message }),
    });
  }

  private concluir(id: number, parcial: Partial<Troca>): void {
    this.trocas.update((t) => t.map((x) => (x.id === id ? { ...x, ...parcial } : x)));
    this.carregando.set(false);
    this.rolarParaFim();
  }

  private rolarParaFim(): void {
    setTimeout(() => this.fim()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'end' }));
  }
}
