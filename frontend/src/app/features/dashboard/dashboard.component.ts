import { Component, input } from '@angular/core';

@Component({ selector: 'app-dashboard', templateUrl: './dashboard.component.html' })
export class DashboardComponent {
  readonly ativos = input.required<number>();
  readonly saldos = input.required<{ unidade: string; quantidade: number }[]>();
  readonly baixos = input.required<number>();
  readonly potencialVendas = input.required<number>();
  readonly moeda = (valor: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor);
}
