import { Component, input } from '@angular/core';

@Component({ selector: 'app-dashboard', templateUrl: './dashboard.component.html' })
export class DashboardComponent {
  readonly ativos = input.required<number>();
  readonly unidades = input.required<number>();
  readonly baixos = input.required<number>();
  readonly valorEstoque = input.required<number>();
  readonly moeda = (valor: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor);
}
