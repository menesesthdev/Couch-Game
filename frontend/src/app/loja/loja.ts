import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { ValorantCoachApi } from '../core/valorant-coach-api';
import { REGIOES, Regiao } from '../core/api.models';

/**
 * Loja pessoal. É a única tela do app que depende de conta conectada — nenhuma tela anônima
 * sabe que esta sessão existe.
 */
@Component({
  selector: 'app-loja',
  imports: [DecimalPipe],
  templateUrl: './loja.html',
  styleUrl: './loja.scss',
})
export class LojaPage {
  private readonly api = inject(ValorantCoachApi);

  protected readonly REGIOES = REGIOES;
  protected readonly regiao = signal<Regiao>('Br');
  protected readonly urlColada = signal('');
  protected readonly conectando = signal(false);

  /** Passo visível do tutorial. Um de cada vez: o campo de colar não serve antes do login. */
  protected readonly etapa = signal(1);
  protected readonly erroConexao = signal<string | null>(null);

  /**
   * Página oficial de login da Riot. O `client_id=riot-client` obriga o redirect a
   * `localhost`, por isso o usuário precisa copiar a URL de volta: o token vem no fragmento
   * (`#`), que o navegador nunca envia a servidor nenhum.
   */
  protected readonly urlLogin =
    'https://auth.riotgames.com/authorize' +
    '?redirect_uri=http%3A%2F%2Flocalhost%2Fredirect' +
    '&client_id=riot-client' +
    // Tem de ser "token id_token": com scope=openid, a Riot recusa response_type=token sozinho
    // ("The OpenID Connect response type cannot have token as the only value").
    '&response_type=token%20id_token' +
    '&scope=openid+link+ban+lol_region+account' +
    '&ui_locales=pt-BR' +
    '&nonce=1';

  protected readonly sessao = rxResource({
    params: () => ({}),
    stream: () => this.api.sessaoLoja(),
  });

  protected readonly conectado = computed(() => this.sessao.value()?.conectado === true);

  // params undefined = requisição nem sai enquanto não houver sessão.
  protected readonly loja = rxResource({
    params: () => (this.conectado() ? {} : undefined),
    stream: () => this.api.minhaLoja(),
  });

  protected readonly dados = computed(() => (this.loja.hasValue() ? this.loja.value() : undefined));

  protected conectar() {
    const url = this.urlColada().trim();
    if (!url || this.conectando()) return;

    this.conectando.set(true);
    this.erroConexao.set(null);
    this.api.conectarLoja(url, this.regiao()).subscribe({
      next: () => {
        this.conectando.set(false);
        this.urlColada.set('');
        this.sessao.reload();
      },
      error: (e: Error) => {
        this.conectando.set(false);
        this.erroConexao.set(e.message);
      },
    });
  }

  protected desconectar() {
    this.api.desconectarLoja().subscribe(() => this.sessao.reload());
  }

  protected abrirLogin() {
    window.open(this.urlLogin, '_blank', 'noopener');
    this.etapa.set(2);
  }

  /** Passo já visto pode ser reaberto; quem já tem a URL na mão pula direto para o 3. */
  protected irPara(n: number) {
    this.etapa.set(n);
  }

  /** "4h 12min" — o suficiente para saber se dá tempo de pensar antes de a loja virar. */
  protected restante(segundos: number | null | undefined): string {
    if (!segundos || segundos <= 0) return 'renovando…';
    const horas = Math.floor(segundos / 3600);
    const minutos = Math.floor((segundos % 3600) / 60);
    if (horas >= 24) return `${Math.floor(horas / 24)}d ${horas % 24}h`;
    return horas > 0 ? `${horas}h ${minutos}min` : `${minutos}min`;
  }

  protected mensagemErroLoja(): string {
    const erro = this.loja.error();
    return erro instanceof Error ? erro.message : 'Não foi possível carregar sua loja.';
  }
}
