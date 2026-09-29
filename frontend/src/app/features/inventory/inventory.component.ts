import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { InventarioFisico, Produto } from '../../shared/models/stock.models';

@Component({
  selector: 'app-inventory',
  imports: [FormsModule, DatePipe],
  templateUrl: './inventory.component.html',
})
export class InventoryComponent implements OnInit {
  private readonly api = inject(EstoqueApiService);
  readonly produtos = input.required<Produto[]>();
  readonly podeMovimentar = input.required<boolean>();
  readonly alterado = output<void>();
  readonly sessaoExpirada = output<void>();
  readonly inventarios = signal<InventarioFisico[]>([]);
  readonly selecionado = signal<InventarioFisico | null>(null);
  readonly ocupados = computed(
    () =>
      new Set(
        this.inventarios()
          .filter((i) => i.situacao === 1)
          .map((i) => i.produtoId),
      ),
  );
  readonly carregando = signal(false);
  readonly salvando = signal(false);
  readonly erro = signal('');
  readonly sucesso = signal('');
  produtoId: number | null = null;
  quantidade: number | null = null;
  motivo = '';

  ngOnInit(): void {
    void this.recarregar();
  }

  private falha(error: unknown): void {
    if (error instanceof HttpErrorResponse && error.status === 401) {
      this.sessaoExpirada.emit();
      return;
    }
    this.erro.set(
      error instanceof HttpErrorResponse
        ? (error.error?.erro ?? error.error?.title ?? 'Não foi possível concluir a operação.')
        : 'Não foi possível concluir a operação.',
    );
  }

  async recarregar(): Promise<void> {
    this.carregando.set(true);
    this.erro.set('');
    try {
      this.inventarios.set(await this.api.inventarios());
    } catch (error) {
      this.falha(error);
    } finally {
      this.carregando.set(false);
    }
  }

  selecionar(inventario: InventarioFisico): void {
    this.selecionado.set(inventario);
    this.quantidade = inventario.quantidadeContada;
    this.motivo = '';
    this.erro.set('');
    this.sucesso.set('');
  }

  async abrir(): Promise<void> {
    if (!this.produtoId || !this.podeMovimentar()) return;
    this.salvando.set(true);
    this.erro.set('');
    try {
      const inventario = await this.api.abrirInventario(this.produtoId);
      await this.recarregar();
      this.selecionar(inventario);
      this.produtoId = null;
      this.sucesso.set('Contagem aberta. Informe a quantidade encontrada.');
    } catch (error) {
      this.falha(error);
    } finally {
      this.salvando.set(false);
    }
  }

  async registrar(): Promise<void> {
    const atual = this.selecionado();
    if (
      !atual ||
      !this.podeMovimentar() ||
      this.quantidade === null ||
      !Number.isInteger(this.quantidade) ||
      this.quantidade < 0
    ) {
      this.erro.set('Informe uma quantidade inteira maior ou igual a zero.');
      return;
    }
    this.salvando.set(true);
    this.erro.set('');
    try {
      const salvo = await this.api.registrarContagem(atual.id, this.quantidade);
      await this.recarregar();
      this.selecionar(salvo);
      this.sucesso.set('Contagem registrada. Confira a diferença antes de confirmar.');
    } catch (error) {
      this.falha(error);
    } finally {
      this.salvando.set(false);
    }
  }

  async confirmar(): Promise<void> {
    const atual = this.selecionado();
    if (
      !atual ||
      atual.quantidadeContada === null ||
      !this.motivo.trim() ||
      !this.podeMovimentar()
    ) {
      this.erro.set('Registre a contagem e informe o motivo da confirmação.');
      return;
    }
    this.salvando.set(true);
    this.erro.set('');
    try {
      await this.api.confirmarInventario(atual.id, this.motivo.trim(), atual.quantidadeContada);
      this.selecionado.set(null);
      await this.recarregar();
      this.alterado.emit();
      this.sucesso.set(
        'Inventário confirmado. A diferença foi registrada no histórico do produto.',
      );
    } catch (error) {
      this.falha(error);
    } finally {
      this.salvando.set(false);
    }
  }

  async cancelar(): Promise<void> {
    const atual = this.selecionado();
    if (!atual || !this.podeMovimentar() || !confirm(`Cancelar a contagem de ${atual.produto}?`))
      return;
    this.salvando.set(true);
    this.erro.set('');
    try {
      await this.api.cancelarInventario(atual.id);
      this.selecionado.set(null);
      await this.recarregar();
      this.sucesso.set('Contagem cancelada.');
    } catch (error) {
      this.falha(error);
    } finally {
      this.salvando.set(false);
    }
  }
}
