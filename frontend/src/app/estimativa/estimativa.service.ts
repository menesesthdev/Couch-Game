import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { EstimativaRequest, EstimativaResponse, RankOpcao } from './estimativa.models';

@Injectable({ providedIn: 'root' })
export class EstimativaService {
  private readonly http = inject(HttpClient);

  ranks(): Observable<RankOpcao[]> {
    return this.http.get<RankOpcao[]>('/api/ranks');
  }

  estimar(request: EstimativaRequest): Observable<EstimativaResponse> {
    return this.http
      .post<EstimativaResponse>('/api/estimativas', request)
      .pipe(catchError((erro: HttpErrorResponse) => throwError(() => new Error(mensagemDeErro(erro)))));
  }
}

function mensagemDeErro(erro: HttpErrorResponse): string {
  if (erro.status === 0) return 'Não foi possível conectar ao servidor.';
  return erro.error?.title ?? 'Algo deu errado. Tente novamente.';
}
