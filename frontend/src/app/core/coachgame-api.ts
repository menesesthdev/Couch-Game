import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { AtoCompetitivo, Estimativa, EstimativaRequest, Perfil, Regiao, SugestaoJogador } from './api.models';

@Injectable({ providedIn: 'root' })
export class CoachgameApi {
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
