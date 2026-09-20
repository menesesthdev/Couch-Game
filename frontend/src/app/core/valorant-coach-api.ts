import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import {
  AtoCompetitivo,
  ConexaoLoja,
  DesempenhoAnalisado,
  DetalhePartida,
  MinhaLoja,
  Estimativa,
  EstimativaRequest,
  Periodo,
  Perfil,
  Regiao,
  SugestaoJogador,
} from './api.models';

@Injectable({ providedIn: 'root' })
export class ValorantCoachApi {
  private readonly http = inject(HttpClient);

  buscar(q: string, regiao?: Regiao): Observable<SugestaoJogador[]> {
    let params = new HttpParams().set('q', q);
    if (regiao) params = params.set('regiao', regiao);
    return this.http.get<SugestaoJogador[]>('/api/jogadores/busca', { params });
  }

  perfil(riotId: string, regiao: Regiao): Observable<Perfil> {
    const params = new HttpParams().set('perfil', riotId).set('regiao', regiao);
    return this.http.get<Perfil>('/api/jogadores/perfil', { params }).pipe(catchError(traduzirErro));
  }

  /** Partidas do período com desempenho e RR. Chamada à parte: o perfil carrega mesmo se esta falhar. */
  desempenho(riotId: string, regiao: Regiao, periodo: Periodo): Observable<DesempenhoAnalisado> {
    const params = new HttpParams().set('perfil', riotId).set('regiao', regiao).set('periodo', periodo);
    return this.http.get<DesempenhoAnalisado>('/api/jogadores/desempenho', { params }).pipe(catchError(traduzirErro));
  }

  /** Scoreboard completo de uma partida. */
  partida(regiao: Regiao, matchId: string): Observable<DetalhePartida> {
    return this.http
      .get<DetalhePartida>(`/api/partidas/${regiao}/${encodeURIComponent(matchId)}`)
      .pipe(catchError(traduzirErro));
  }

  // ---- Loja: a única área que exige conta conectada. O cookie de sessão é HttpOnly e
  // viaja sozinho por ser mesma origem (o nginx faz o proxy de /api). ----

  sessaoLoja(): Observable<ConexaoLoja> {
    return this.http.get<ConexaoLoja>('/api/loja/sessao').pipe(catchError(() => of({ conectado: false, expiraEm: null })));
  }

  conectarLoja(urlRedirecionamento: string, regiao: Regiao): Observable<ConexaoLoja> {
    return this.http
      .post<ConexaoLoja>('/api/loja/conectar', { urlRedirecionamento, regiao })
      .pipe(catchError(traduzirErro));
  }

  minhaLoja(): Observable<MinhaLoja> {
    return this.http.get<MinhaLoja>('/api/loja').pipe(catchError(traduzirErro));
  }

  desconectarLoja(): Observable<void> {
    return this.http.post<void>('/api/loja/desconectar', {});
  }

  /** Ato em andamento, ou null entre atos / se o calendário estiver fora do ar. */
  atoAtual(): Observable<AtoCompetitivo | null> {
    return this.http.get<AtoCompetitivo | null>('/api/calendario/ato-atual').pipe(
      map((ato) => ato ?? null),
      catchError(() => of(null)),
    );
  }

  estimar(request: EstimativaRequest): Observable<Estimativa> {
    return this.http.post<Estimativa>('/api/estimativas', request).pipe(catchError(traduzirErro));
  }
}

function traduzirErro(erro: HttpErrorResponse) {
  const mensagem =
    erro.status === 0
      ? 'Não foi possível conectar ao servidor.'
      : (erro.error?.title ?? 'Algo deu errado. Tente novamente.');
  return throwError(() => new Error(mensagem));
}
