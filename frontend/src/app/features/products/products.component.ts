import { PosicaoEstoque } from '../../shared/models/location.models';
import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Produto, TipoMovimento } from '../../shared/models/stock.models';

@Component({
  selector: 'app-products',
  imports: [FormsModule],
  templateUrl: './products.component.html',
})
export class ProductsComponent {
  readonly produtosFiltrados = input.required<Produto[]>();
  readonly totalProdutos = input.required<number>();
  readonly posicoes = input<PosicaoEstoque[]>([]);
  readonly filtroPosicao = input('todos');
  readonly filtroPosicaoChange = output<string>();
  readonly busca = input.required<string>();
  readonly somenteAtivos = input.required<boolean>();
  readonly carregando = input.required<boolean>();
  readonly podeMovimentar = input.required<boolean>();
  readonly podeGerenciar = input.required<boolean>();
  readonly buscaChange = output<string>();
  readonly somenteAtivosChange = output<boolean>();
  readonly atualizar = output<void>();
  readonly movimentar = output<{ produto: Produto; tipo: TipoMovimento }>();
  readonly historico = output<Produto>();
  readonly editar = output<Produto>();
  readonly desativar = output<Produto>();
  readonly moeda = (valor: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor);
}
