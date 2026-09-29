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
  readonly historicoPedidoId = signal<number | null>(null);
  readonly historico = signal<RecebimentoPedidoCompra[]>([]);
  readonly carregandoHistorico = signal(false);

  status(p: PedidoCompra): string {
    if (p.situacao === 2) return 'Recebido';
    if (p.situacao === 3) return 'Cancelado';
    if (p.situacao === 5) return 'Saldo cancelado após recebimento parcial';
    if (p.situacao === 4) return 'Parcialmente recebido';
    return 'Aberto';
  }
  data(valor: string): string { return new Date(valor).toLocaleString('pt-BR'); }
  ngOnInit(): void { void this.recarregar(); }
  async recarregar(): Promise<void> {
    this.carregando.set(true); this.erro.set('');
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
  async receber(p: PedidoCompra): Promise<void> {
    const itens = p.itens.filter(item => this.quantidade(p.id, item.produtoId) > 0)
      .map(item => ({ produtoId: item.produtoId, quantidade: this.quantidade(p.id, item.produtoId) }));
    if (!itens.length || p.itens.some(item => {
      const quantidade = this.quantidade(p.id, item.produtoId);
      return !Number.isSafeInteger(quantidade) || quantidade < 0 || quantidade > item.quantidade - item.quantidadeRecebida;
    })) {
      this.erro.set('Informe ao menos um recebimento com quantidade inteira positiva, sem exceder o saldo pendente.');
      return;
    }
    if (!confirm(`Confirmar recebimento de ${itens.reduce((total, item) => total + item.quantidade, 0)} unidade(s) do pedido #${p.id}?`)) return;
    this.processando.set(true); this.erro.set('');
    try {
      await this.api.receberPedidoCompra(p.id, itens);
      this.recebimentos.set({});
      this.sucesso.set(`Recebimento do pedido #${p.id} registrado no estoque.`);
      await this.recarregar();
      if (this.historicoPedidoId() === p.id) this.historico.set(await this.api.recebimentosPedidoCompra(p.id));
      this.alterado.emit();
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
