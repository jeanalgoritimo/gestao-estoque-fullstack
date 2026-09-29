import { Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Fornecedor, Produto } from '../../shared/models/stock.models';

@Component({
  selector: 'app-replenishment',
  imports: [FormsModule],
  templateUrl: './replenishment.component.html',
})
export class ReplenishmentComponent {
  readonly produtos = input.required<Produto[]>();
  readonly fornecedores = input.required<Fornecedor[]>();
  readonly carregando = input(false);
  readonly atualizar = output<void>();
  readonly busca = signal('');
  readonly fornecedorId = signal<number | null>(null);
  readonly apenasComFornecedor = signal(false);
  readonly quantidades = signal<Record<number, number>>({});
  readonly erro = signal('');

  readonly candidatos = computed(() => this.produtos().filter(p => p.ativo && p.estoque <= p.estoqueMinimo));
  readonly filtrados = computed(() => {
    const termo = this.busca().trim().toLocaleLowerCase('pt-BR');
    return this.candidatos().filter(p =>
      (!termo || `${p.nome} ${p.categoria} ${p.id}`.toLocaleLowerCase('pt-BR').includes(termo)) &&
      (this.fornecedorId() === null || p.fornecedorId === this.fornecedorId()) &&
      (!this.apenasComFornecedor() || this.fornecedorAtivo(p)));
  });
  readonly semFornecedor = computed(() => this.candidatos().filter(p => !this.fornecedorAtivo(p)).length);

  fornecedorAtivo(p: Produto): boolean {
    return this.fornecedores().some(f => f.id === p.fornecedorId && f.ativo);
  }
  sugerida(p: Produto): number {
    // O mínimo é o gatilho. A sugestão recoloca o saldo uma unidade acima dele.
    return Math.max(1, p.estoqueMinimo + 1 - p.estoque);
  }
  quantidade(p: Produto): number {
    return this.quantidades()[p.id] ?? this.sugerida(p);
  }
  alterarQuantidade(p: Produto, valor: number): void {
    this.quantidades.update(atual => ({ ...atual, [p.id]: valor }));
    this.erro.set('');
  }
  restaurar(): void {
    this.quantidades.set({});
    this.erro.set('');
  }
  exportarCsv(): void {
    const itens = this.filtrados();
    if (!itens.length || itens.some(p => !Number.isSafeInteger(this.quantidade(p)) || this.quantidade(p) < 1)) {
      this.erro.set('Informe quantidades inteiras maiores que zero para todos os produtos exibidos.');
      return;
    }
    const celula = (valor: string | number): string => {
      let texto = String(valor);
      if (/^\s*[=+\-@]/.test(texto)) texto = `'${texto}`;
      return `"${texto.replace(/"/g, '""')}"`;
    };
    const linhas = [
      ['Código', 'Produto', 'Categoria', 'Fornecedor', 'Saldo atual', 'Estoque mínimo', 'Quantidade sugerida', 'Quantidade ajustada'],
      ...itens.map(p => [p.id, p.nome, p.categoria,
        this.fornecedorAtivo(p) ? p.fornecedor ?? '' : 'Fornecedor pendente',
        p.estoque, p.estoqueMinimo, this.sugerida(p), this.quantidade(p)]),
    ];
    const url = URL.createObjectURL(new Blob(['\ufeff' + linhas.map(l => l.map(celula).join(';')).join('\r\n')], { type: 'text/csv;charset=utf-8' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = `sugestoes-reposicao-${new Date().toISOString().slice(0, 10)}.csv`;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}
