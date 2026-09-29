import { Component, input, output } from '@angular/core';
import { Categoria } from '../../shared/models/stock.models';

@Component({ selector: 'app-categories', templateUrl: './categories.component.html' })
export class CategoriesComponent {
  readonly categorias = input.required<Categoria[]>();
  readonly podeGerenciar = input.required<boolean>();
  readonly atualizar = output<void>();
  readonly editar = output<Categoria>();
  readonly alternarAtiva = output<Categoria>();
}
