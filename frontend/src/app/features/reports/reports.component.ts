import { Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Produto } from '../../shared/models/stock.models';

@Component({
  selector: 'app-reports',
  imports: [FormsModule],
  templateUrl: './reports.component.html',
})
export class ReportsComponent {
  readonly produtos = input.required<Produto[]>();
  readonly carregando = input(false);
  readonly atualizar = output<void>();
  readonly busca = signal('');
  readonly categoria = signal('');
  readonly somenteBaixos = signal(false);
  readonly categorias = computed(() => [...new Set(this.produtos().filter(p => p.ativo).map(p => p.categoria))].sort((a, b) => a.localeCompare(b, 'pt-BR')));
  readonly filtrados = computed(() => {
    const termo = this.busca().trim().toLocaleLowerCase('pt-BR');
    return this.produtos().filter(p => p.ativo &&
      (!termo || `${p.nome} ${p.categoria} ${p.id}`.toLocaleLowerCase('pt-BR').includes(termo)) &&
      (!this.categoria() || p.categoria === this.categoria()) &&
      (!this.somenteBaixos() || p.estoqueBaixo));
  });
  readonly unidades = computed(() => this.filtrados().reduce((total, p) => total + p.estoque, 0));
  readonly baixos = computed(() => this.filtrados().filter(p => p.estoqueBaixo).length);
  readonly potencial = computed(() => this.filtrados().reduce((total, p) => total + p.estoque * p.preco, 0));

  moeda(valor: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor);
  }

  exportarCsv(): void {
    const celula = (valor: string | number): string => {
      let texto = String(valor);
      // Evita que planilhas interpretem nomes/categorias como fórmulas.
      if (/^\s*[=+\-@]/.test(texto)) texto = `'${texto}`;
      return `"${texto.replace(/"/g, '""')}"`;
    };
    const linhas = [
      ['Código', 'Produto', 'Categoria', 'Estoque', 'Mínimo', 'Estoque baixo', 'Preço de venda (R$)', 'Potencial de venda (R$)'],
      ...this.filtrados().map(p => [p.id, p.nome, p.categoria, p.estoque, p.estoqueMinimo,
        p.estoqueBaixo ? 'Sim' : 'Não', p.preco.toFixed(2).replace('.', ','), (p.estoque * p.preco).toFixed(2).replace('.', ',')]),
    ];
    const csv = '\ufeff' + linhas.map(linha => linha.map(celula).join(';')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = `posicao-estoque-${new Date().toISOString().slice(0, 10)}.csv`;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}
