import { Component, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { EstimativaRequest, RankOpcao, Regiao } from '../estimativa/estimativa.models';

function daquiA(dias: number): string {
  const data = new Date();
  data.setDate(data.getDate() + dias);
  return data.toISOString().slice(0, 10);
}

/** Caixa de entrada no estilo "composer" do ChatGPT: perfil + região + meta. */
@Component({
  selector: 'app-composer',
  imports: [ReactiveFormsModule],
  templateUrl: './composer.html',
  styleUrl: './composer.scss',
})
export class Composer {
  readonly ranks = input.required<RankOpcao[]>();
  readonly carregando = input(false);
  readonly enviar = output<EstimativaRequest>();

  protected readonly regioes: { valor: Regiao; nome: string }[] = [
    { valor: 'Br', nome: 'BR' },
    { valor: 'Latam', nome: 'LATAM' },
    { valor: 'Na', nome: 'NA' },
    { valor: 'Eu', nome: 'EU' },
    { valor: 'Ap', nome: 'AP' },
    { valor: 'Kr', nome: 'KR' },
  ];

  protected readonly amanha = daquiA(1);

  protected readonly form = new FormGroup({
    perfil: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    regiao: new FormControl<Regiao>('Br', { nonNullable: true }),
    rankAlvo: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    dataLimite: new FormControl(daquiA(30), { nonNullable: true, validators: [Validators.required] }),
  });

  protected submeter(): void {
    if (this.form.invalid || this.carregando()) {
      this.form.markAllAsTouched();
      return;
    }
    this.enviar.emit(this.form.getRawValue());
  }
}
