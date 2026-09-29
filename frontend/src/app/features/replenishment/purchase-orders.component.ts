import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, output, signal, input } from '@angular/core';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { PedidoCompra } from '../../shared/models/stock.models';

@Component({ selector: 'app-purchase-orders', templateUrl: './purchase-orders.component.html' })
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
  async receber(p: PedidoCompra): Promise<void> {
    if (!confirm(`Confirmar recebimento integral do pedido #${p.id}? O estoque será atualizado.`)) return;
    this.processando.set(true); this.erro.set('');
    try {
      await this.api.receberPedidoCompra(p.id);
      this.sucesso.set(`Pedido #${p.id} recebido e registrado no estoque.`);
      await this.recarregar(); this.alterado.emit();
    } catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.processando.set(false); }
  }
  async cancelar(p: PedidoCompra): Promise<void> {
    if (!confirm(`Cancelar pedido #${p.id}?`)) return;
    this.processando.set(true); this.erro.set('');
    try { await this.api.cancelarPedidoCompra(p.id); this.sucesso.set(`Pedido #${p.id} cancelado.`); await this.recarregar(); }
    catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.processando.set(false); }
  }
}
