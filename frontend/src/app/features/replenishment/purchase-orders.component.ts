import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, output, signal, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { PedidoCompra, RecebimentoPedidoCompra } from '../../shared/models/stock.models';

@Component({ selector: 'app-purchase-orders', imports: [FormsModule], templateUrl: './purchase-orders.component.html' })
export class PurchaseOrdersComponent implements OnInit {
  private readonly api = inject(EstoqueApiService);
  readonly podeReceber = input(false);
  readonly administrador = input(false);
  readonly alterado = output<void>();
  readonly pedidos = signal<PedidoCompra[]>([]);
  readonly carregando = signal(false);
  readonly processando = signal(false);
  readonly erro = signal('');
  readonly sucesso = signal('');
  readonly recebimentos = signal<Record<string, number>>({});
  readonly custos = signal<Record<string, number | null>>({});
  readonly historicoPedidoId = signal<number | null>(null);
  readonly historico = signal<RecebimentoPedidoCompra[]>([]);
  readonly carregandoHistorico = signal(false);
  editando: PedidoCompra | null = null;
  itensEdicao: { produtoId: number; produto: string; unidade: string; quantidade: number }[] = [];

  status(p: PedidoCompra): string {
    if (p.situacao === 6) return 'Rascunho';
    if (p.situacao === 2) return 'Recebido';
    if (p.situacao === 3) return 'Cancelado';
    if (p.situacao === 5) return 'Saldo cancelado após recebimento parcial';
    if (p.situacao === 4) return 'Parcialmente recebido';
    return 'Aberto';
  }
  data(valor: string): string { return new Date(valor).toLocaleString('pt-BR'); }
  ngOnInit(): void { void this.recarregar(); }
  async recarregar(): Promise<void> {
    this.editando = null; this.carregando.set(true); this.erro.set('');
    try { this.pedidos.set(await this.api.pedidosCompra()); }
    catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.carregando.set(false); }
  }
  private mensagem(error: unknown): string {
    if (error instanceof HttpErrorResponse) return error.error?.erro ?? `Falha na requisição (${error.status}).`;
    return 'Não foi possível concluir a operação.';
  }
  async abrirHistorico(id: number): Promise<void> {
    if (this.historicoPedidoId() === id) { this.historicoPedidoId.set(null); return; }
    this.historicoPedidoId.set(id);
    this.historico.set([]);
    this.carregandoHistorico.set(true);
    this.erro.set('');
    try {
      const recebimentos = await this.api.recebimentosPedidoCompra(id);
      if (this.historicoPedidoId() === id) this.historico.set(recebimentos);
    } catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.carregandoHistorico.set(false); }
  }
  quantidade(pedidoId: number, produtoId: number): number {
    return this.recebimentos()[`${pedidoId}-${produtoId}`] ?? 0;
  }
  definirQuantidade(pedidoId: number, produtoId: number, quantidade: number): void {
    this.recebimentos.update(atual => ({ ...atual, [`${pedidoId}-${produtoId}`]: quantidade }));
    this.erro.set('');
  }
  custo(pedidoId: number, produtoId: number): number | null {
    return this.custos()[`${pedidoId}-${produtoId}`] ?? null;
  }
  definirCusto(pedidoId: number, produtoId: number, valor: number | null): void {
    this.custos.update(atual => ({ ...atual, [`${pedidoId}-${produtoId}`]: valor }));
    this.erro.set('');
  }
  moeda(valor: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor);
  }
  async receber(p: PedidoCompra): Promise<void> {
    const itens = p.itens.filter(item => this.quantidade(p.id, item.produtoId) > 0)
      .map(item => ({ produtoId: item.produtoId, quantidade: this.quantidade(p.id, item.produtoId), custoUnitario: this.custo(p.id, item.produtoId)! }));
    if (!itens.length || p.itens.some(item => {
      const quantidade = this.quantidade(p.id, item.produtoId);
      const custo = this.custo(p.id, item.produtoId);
      return !Number.isSafeInteger(quantidade) || quantidade < 0 || quantidade > item.quantidade - item.quantidadeRecebida ||
        (quantidade > 0 && (custo === null || !Number.isFinite(custo) || custo <= 0 ||
          custo > 999999999999.9999 || Math.abs(Math.round(custo * 10000) - custo * 10000) > 0.0000001));
    })) {
      this.erro.set('Informe quantidades inteiras dentro do saldo pendente e custo unitário de compra positivo (até 4 casas decimais) para os itens recebidos.');
      return;
    }
    const total = itens.reduce((valor, item) => valor + item.quantidade * item.custoUnitario, 0);
    if (!confirm(`Confirmar o recebimento dos itens selecionados do pedido #${p.id}, custo de compra ${this.moeda(total)}?`)) return;
    this.processando.set(true); this.erro.set('');
    try {
      await this.api.receberPedidoCompra(p.id, itens);
      this.recebimentos.set({});
      this.custos.set({});
      this.sucesso.set(`Recebimento do pedido #${p.id} registrado no estoque.`);
      await this.recarregar();
      if (this.historicoPedidoId() === p.id) this.historico.set(await this.api.recebimentosPedidoCompra(p.id));
      this.alterado.emit();
    } catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.processando.set(false); }
  }
  editar(p: PedidoCompra): void {
    this.editando = p; this.itensEdicao = p.itens.map(i => ({ produtoId: i.produtoId, produto: i.produto, unidade: i.unidade, quantidade: i.quantidade }));
    this.erro.set(''); this.sucesso.set('');
  }
  removerItem(id: number): void { this.itensEdicao = this.itensEdicao.filter(i => i.produtoId !== id); }
  async salvarRascunho(): Promise<void> {
    const p = this.editando;
    if (!p || this.processando()) return;
    if (!this.itensEdicao.length || this.itensEdicao.some(i => !Number.isInteger(i.quantidade) || i.quantidade < 1 || i.quantidade > 2147483647)) {
      this.erro.set('Mantenha pelo menos um item com quantidade inteira positiva.'); return;
    }
    this.processando.set(true); this.erro.set(''); this.sucesso.set('');
    try {
      await this.api.editarRascunhoCompra(p.id, this.itensEdicao.map(i => ({ produtoId: i.produtoId, quantidade: i.quantidade })), p.versao);
      this.editando = null; this.sucesso.set('Rascunho atualizado. Revise antes de confirmar.'); await this.recarregar();
    } catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.processando.set(false); }
  }
  async confirmar(p: PedidoCompra): Promise<void> {
    if (this.processando()) return;
    this.processando.set(true); this.erro.set(''); this.sucesso.set('');
    try {
      await this.api.confirmarRascunhoCompra(p.id, p.versao);
      this.sucesso.set(`Pedido #${p.id} aberto. Pode registrar recebimentos.`); await this.recarregar();
    } catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.processando.set(false); }
  }
  async cancelar(p: PedidoCompra): Promise<void> {
    const motivo = prompt(p.situacao === 4
      ? `Motivo para cancelar o saldo pendente do pedido #${p.id}:`
      : `Motivo para cancelar o pedido #${p.id}:`);
    if (motivo === null) return;
    if (!motivo.trim() || motivo.trim().length > 150) {
      this.erro.set('Informe um motivo de até 150 caracteres.');
      return;
    }
    if (!confirm(p.situacao === 4
      ? `Cancelar apenas o saldo pendente do pedido #${p.id}? As entradas já recebidas serão preservadas.`
      : `Cancelar pedido #${p.id}?`)) return;
    this.processando.set(true); this.erro.set('');
    try { await this.api.cancelarPedidoCompra(p.id, motivo.trim()); this.sucesso.set(p.situacao === 4 ? `Saldo pendente do pedido #${p.id} cancelado.` : `Pedido #${p.id} cancelado.`); await this.recarregar(); }
    catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.processando.set(false); }
  }
}
