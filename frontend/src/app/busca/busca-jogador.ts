import { Component, ElementRef, computed, inject, input, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { CoachgameApi } from '../core/coachgame-api';
import { BuscasRecentes } from '../core/buscas-recentes';
import { REGIOES, Regiao } from '../core/api.models';
import { iconeRank, nomeTier } from '../core/ranks';
import { parseRiotId, segmentoPerfil } from '../core/riot-id';

interface Opcao {
  nome: string;
  tag: string;
  regiao: Regiao;
  tier?: string;
  rr?: number;
  origem: 'direto' | 'recente' | 'jogador';
}

/**
 * Campo de busca com autocomplete estilo tracker: enquanto digita, sugere jogadores já
 * consultados (API) e buscas recentes (localStorage). Enter abre o perfil.
 */
@Component({
  selector: 'app-busca-jogador',
  templateUrl: './busca-jogador.html',
  styleUrl: './busca-jogador.scss',
  host: {
    '[class.compacta]': 'compacta()',
    '(document:click)': 'fecharSeForaDe($event)',
  },
})
export class BuscaJogador {
  readonly compacta = input(false);

  private readonly api = inject(CoachgameApi);
  private readonly recentes = inject(BuscasRecentes);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly regioes = REGIOES;
  protected readonly iconeRank = iconeRank;
  protected readonly nomeTier = nomeTier;

  protected readonly termo = signal('');
  protected readonly regiao = signal<Regiao>('Br');
  protected readonly aberta = signal(false);
  protected readonly ativa = signal(0);
  protected readonly erro = signal('');

  private readonly sugestoes = toSignal(
    toObservable(computed(() => ({ termo: this.termo().trim(), regiao: this.regiao() }))).pipe(
      debounceTime(200),
      distinctUntilChanged((a, b) => a.termo === b.termo && a.regiao === b.regiao),
      switchMap(({ termo, regiao }) =>
        termo.length < 2 || termo.includes('tracker.gg')
          ? of([])
          : this.api.buscar(termo, regiao).pipe(catchError(() => of([]))),
      ),
      map((lista) => lista.map<Opcao>((j) => ({ ...j, tier: j.rank.tier, rr: j.rank.rr, origem: 'jogador' }))),
    ),
    { initialValue: [] as Opcao[] },
  );

  protected readonly opcoes = computed<Opcao[]>(() => {
    const termo = this.termo().trim().toLowerCase();
    const chave = (o: Opcao) => `${o.nome}#${o.tag}`.toLowerCase();

    const direto = parseRiotId(this.termo());
    const recentes = this.recentes
      .itens()
      .filter((r) => !termo || `${r.nome}#${r.tag}`.toLowerCase().startsWith(termo))
      .map<Opcao>((r) => ({ ...r, origem: 'recente' }));

    const vistas = new Set<string>();
    const lista: Opcao[] = [];
    for (const o of [...recentes, ...this.sugestoes()]) {
      if (!vistas.has(chave(o))) {
        vistas.add(chave(o));
        lista.push(o);
      }
    }
    // Riot ID completo digitado/colado e ainda não sugerido: oferece abrir direto.
    if (direto && !vistas.has(`${direto.nome}#${direto.tag}`.toLowerCase())) {
      lista.unshift({ ...direto, regiao: this.regiao(), origem: 'direto' });
    }
    return lista.slice(0, 8);
  });

  protected digitar(valor: string): void {
    this.termo.set(valor);
    this.erro.set('');
    this.ativa.set(0);
    this.aberta.set(true);
  }

  protected teclar(evento: KeyboardEvent): void {
    const total = this.opcoes().length;
    switch (evento.key) {
      case 'ArrowDown':
        evento.preventDefault();
        this.aberta.set(true);
        if (total) this.ativa.update((i) => (i + 1) % total);
        break;
      case 'ArrowUp':
        evento.preventDefault();
        if (total) this.ativa.update((i) => (i - 1 + total) % total);
        break;
      case 'Escape':
        this.aberta.set(false);
        break;
      case 'Enter':
        evento.preventDefault();
        this.confirmar();
        break;
    }
  }

  protected confirmar(): void {
    const opcao = this.aberta() ? this.opcoes()[this.ativa()] : undefined;
    if (opcao) return this.abrir(opcao);

    const direto = parseRiotId(this.termo());
    if (direto) return this.abrir({ ...direto, regiao: this.regiao(), origem: 'direto' });
    this.erro.set('Use o formato nome#tag ou cole o link do tracker.gg.');
  }

  protected abrir(o: Opcao): void {
    this.recentes.registrar({ nome: o.nome, tag: o.tag, regiao: o.regiao, tier: o.tier });
    this.aberta.set(false);
    this.termo.set('');
    this.router.navigate(['/perfil', o.regiao.toLowerCase(), segmentoPerfil(o)]);
  }

  protected removerRecente(evento: Event, o: Opcao): void {
    evento.stopPropagation();
    const item = this.recentes.itens().find((r) => r.nome === o.nome && r.tag === o.tag);
    if (item) this.recentes.remover(item);
  }

  protected fecharSeForaDe(evento: MouseEvent): void {
    if (!this.host.nativeElement.contains(evento.target as Node)) this.aberta.set(false);
  }

  protected regiaoNome(r: Regiao): string {
    return this.regioes.find((x) => x.valor === r)?.nome ?? r;
  }
}
