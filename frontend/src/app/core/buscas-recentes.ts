import { Injectable, signal } from '@angular/core';
import { Regiao } from './api.models';

export interface BuscaRecente {
  nome: string;
  tag: string;
  regiao: Regiao;
  tier?: string;
}

const CHAVE = 'coachgame.buscas-recentes';
const MAXIMO = 6;

/** Últimos perfis abertos neste navegador — conveniência local, não é estado da aplicação. */
@Injectable({ providedIn: 'root' })
export class BuscasRecentes {
  readonly itens = signal<BuscaRecente[]>(this.ler());

  registrar(busca: BuscaRecente): void {
    const mesma = (b: BuscaRecente) =>
      b.nome.toLowerCase() === busca.nome.toLowerCase() && b.tag.toLowerCase() === busca.tag.toLowerCase();
    this.itens.update((atual) => [busca, ...atual.filter((b) => !mesma(b))].slice(0, MAXIMO));
    this.gravar();
  }

  remover(busca: BuscaRecente): void {
    this.itens.update((atual) => atual.filter((b) => b !== busca));
    this.gravar();
  }

  private ler(): BuscaRecente[] {
    try {
      return JSON.parse(localStorage.getItem(CHAVE) ?? '[]');
    } catch {
      return [];
    }
  }

  private gravar(): void {
    try {
      localStorage.setItem(CHAVE, JSON.stringify(this.itens()));
    } catch {
      /* armazenamento indisponível (modo privado etc.) — segue sem histórico */
    }
  }
}
