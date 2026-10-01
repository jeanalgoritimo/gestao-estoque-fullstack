import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { Fornecedor, SugestaoReposicao } from '../../shared/models/stock.models';

@Component({ selector: 'app-replenishment', imports: [FormsModule], templateUrl: './replenishment.component.html' })
export class ReplenishmentComponent implements OnInit {
  private readonly api = inject(EstoqueApiService);
  readonly fornecedores = input.required<Fornecedor[]>();
  readonly administrador = input(false);
  readonly criado = output<number>();
  readonly carregando = signal(false);
  readonly processando = signal(false);
  readonly candidatos = signal<SugestaoReposicao[]>([]);
  readonly busca = signal('');
  readonly fornecedorId = signal<number | null>(null);
  readonly apenasComFornecedor = signal(false);
  readonly quantidades = signal<Record<number, number>>({});
  readonly erro = signal('');
  readonly filtrados = computed(() => {
    const termo = this.busca().trim().toLocaleLowerCase('pt-BR');
    return this.candidatos().filter(p =>
      (!termo || `${p.nome} ${p.categoria} ${p.produtoId}`.toLocaleLowerCase('pt-BR').includes(termo)) &&
      (this.fornecedorId() === null || p.fornecedorId === this.fornecedorId()) &&
      (!this.apenasComFornecedor() || p.fornecedorAtivo));
  });
  readonly semFornecedor = computed(() => this.candidatos().filter(p => !p.fornecedorAtivo).length);
  ngOnInit(): void { void this.recarregar(); }
  private mensagem(error: unknown): string {
    if (error instanceof HttpErrorResponse) return error.error?.erro ?? `Falha na requisição (${error.status}).`;
    return 'Não foi possível concluir a operação.';
  }
  async recarregar(): Promise<void> {
    if (this.carregando() || this.processando()) return;
    this.carregando.set(true); this.erro.set(''); this.candidatos.set([]); this.quantidades.set({});
    try { this.candidatos.set(await this.api.sugestoesReposicao()); }
    catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.carregando.set(false); }
  }
  quantidade(p: SugestaoReposicao): number { return this.quantidades()[p.produtoId] ?? p.sugerida; }
  alterarQuantidade(p: SugestaoReposicao, valor: number): void {
    this.quantidades.update(atual => ({ ...atual, [p.produtoId]: valor })); this.erro.set('');
  }
  restaurar(): void { this.quantidades.set({}); this.erro.set(''); }
  private quantidadesValidas(): boolean {
    return this.filtrados().every(p => Number.isInteger(this.quantidade(p)) && this.quantidade(p) >= 0 && this.quantidade(p) <= 2147483647);
  }
  async solicitarPedido(): Promise<void> {
    if (this.processando() || this.carregando() || !this.administrador()) return;
    const itens = this.filtrados().filter(p => this.quantidade(p) > 0);
    const fornecedorId = this.fornecedorId();
    if (!fornecedorId || !itens.length || itens.length > 100 || !this.quantidadesValidas() ||
      itens.some(p => !p.fornecedorAtivo || p.fornecedorId !== fornecedorId)) {
      this.erro.set('Selecione um fornecedor ativo e de 1 a 100 itens com quantidades inteiras positivas. Use zero para excluir um item.'); return;
    }
    this.processando.set(true); this.erro.set('');
    try {
      const pedido = await this.api.criarPedidoCompra({ fornecedorId, rascunho: true,
        itens: itens.map(p => ({ produtoId: p.produtoId, quantidade: this.quantidade(p) })) });
      this.criado.emit(pedido.id);
    } catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.processando.set(false); }
  }
  exportarCsv(): void {
    const itens = this.filtrados();
    if (!itens.length || !this.quantidadesValidas()) { this.erro.set('Informe quantidades inteiras de zero a 2147483647.'); return; }
    const celula = (valor: string | number): string => {
      let texto = String(valor); if (/^\s*[=+\-@]/.test(texto)) texto = `'${texto}`;
      return `"${texto.replace(/"/g, '""')}"`;
    };
    const linhas = [
      ['Código', 'Produto', 'Categoria', 'Fornecedor', 'Saldo físico', 'Reservado', 'Disponível', 'Mínimo', 'Compra pendente', 'Em rascunhos', 'Sugerida', 'Ajustada'],
      ...itens.map(p => [p.produtoId, p.nome, p.categoria, p.fornecedorAtivo ? p.fornecedor ?? '' : 'Fornecedor pendente',
        p.estoque, p.reservado, p.disponivel, p.minimo, p.comprasPendentes, p.emRascunhos, p.sugerida, this.quantidade(p)]),
    ];
    const url = URL.createObjectURL(new Blob(['\ufeff' + linhas.map(l => l.map(celula).join(';')).join('\r\n')], { type: 'text/csv;charset=utf-8' }));
    const link = document.createElement('a'); link.href = url; link.download = `sugestoes-reposicao-${new Date().toISOString().slice(0, 10)}.csv`; link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}
