import { Pipe, PipeTransform } from '@angular/core';

/** "agora", "há 5 min", "há 3 h", "ontem", "há 4 dias". */
@Pipe({ name: 'tempoRelativo' })
export class TempoRelativoPipe implements PipeTransform {
  transform(valor: string | Date | null | undefined): string {
    if (!valor) return '';
    const minutos = Math.floor((Date.now() - new Date(valor).getTime()) / 60_000);
    if (minutos < 1) return 'agora';
    if (minutos < 60) return `há ${minutos} min`;
    const horas = Math.floor(minutos / 60);
    if (horas < 24) return `há ${horas} h`;
    const dias = Math.floor(horas / 24);
    return dias === 1 ? 'ontem' : `há ${dias} dias`;
  }
}
