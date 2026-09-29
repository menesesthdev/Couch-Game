import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { Mira, TipoMira } from '../core/api.models';
import { ValorantCoachApi } from '../core/valorant-coach-api';
import { MiraPreview } from './mira-preview';

/** As coleções da aba. `null` é "todas". */
interface Colecao {
  id: TipoMira | null;
  nome: string;
}

const COLECOES: Colecao[] = [
  { id: null, nome: 'Todas' },
  { id: 'Cruz', nome: 'Cruz' },
  { id: 'Ponto', nome: 'Ponto' },
  { id: 'CruzComPonto', nome: 'Cruz com ponto' },
];

@Component({
  selector: 'app-crosshair',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, MiraPreview],
  templateUrl: './crosshair.html',
  styleUrl: './crosshair.scss',
})
export class CrosshairPage {
  private readonly api = inject(ValorantCoachApi);

  protected readonly colecoes = COLECOES;
  protected readonly colecao = signal<TipoMira | null>(null);
  protected readonly busca = signal('');

  /** Qual mira acabou de ser copiada, para o botão confirmar sem abrir diálogo nenhum. */
  protected readonly copiada = signal<number | null>(null);
  private relogioCopia?: ReturnType<typeof setTimeout>;

  /**
   * Carrega a galeria inteira uma vez só. São poucas dezenas de miras, então filtrar aqui sai
   * instantâneo — ir ao servidor a cada tecla ou a cada troca de coleção só acrescentaria espera.
   */
  protected readonly miras = rxResource({
    params: () => ({}),
    stream: () => this.api.listarMiras(),
  });

  protected readonly todas = computed<Mira[]>(() => this.miras.value() ?? []);

  protected readonly visiveis = computed(() => {
    const tipo = this.colecao();
    const termo = this.busca().trim().toLocaleLowerCase('pt-BR');

    return this.todas().filter(
      (m) =>
        (tipo === null || m.tipo === tipo) &&
        (termo === '' || m.nome.toLocaleLowerCase('pt-BR').includes(termo)),
    );
  });

  /** Quantas miras cada coleção tem, para o número aparecer na própria pílula. */
  protected readonly contagens = computed(() => {
    const todas = this.todas();
    const por = (id: TipoMira | null) => (id === null ? todas.length : todas.filter((m) => m.tipo === id).length);
    return new Map(COLECOES.map((c) => [c.id, por(c.id)]));
  });

  protected escolher(id: TipoMira | null): void {
    this.colecao.set(id);
  }

  protected aoDigitar(evento: Event): void {
    this.busca.set((evento.target as HTMLInputElement).value);
  }

  protected async copiar(mira: Mira): Promise<void> {
    try {
      await navigator.clipboard.writeText(mira.codigo);
    } catch {
      // Navegador sem permissão de área de transferência: seleciona o código para o
      // jogador copiar no muque, em vez de deixar o clique sem resposta nenhuma.
      this.selecionar(mira.id);
      return;
    }

    this.copiada.set(mira.id);
    clearTimeout(this.relogioCopia);
    this.relogioCopia = setTimeout(() => this.copiada.set(null), 2000);
  }

  private selecionar(id: number): void {
    const campo = document.getElementById(`codigo-${id}`) as HTMLInputElement | null;
    campo?.select();
  }
}
