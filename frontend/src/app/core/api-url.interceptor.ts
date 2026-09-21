import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

/**
 * Prefixa as chamadas a /api com o endereço da API do ambiente. Com a API em outro domínio,
 * o cookie da sessão da loja só vai junto com withCredentials.
 */
export const apiUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (!environment.apiUrl || !req.url.startsWith('/api')) return next(req);
  return next(req.clone({ url: environment.apiUrl + req.url, withCredentials: true }));
};
