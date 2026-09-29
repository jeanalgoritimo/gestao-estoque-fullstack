import { HttpErrorResponse } from '@angular/common/http';
import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { Fornecedor } from '../../shared/models/stock.models';

@Component({ selector: 'app-suppliers', imports: [FormsModule], templateUrl: './suppliers.component.html' })
export class SuppliersComponent {
  readonly fornecedores = input.required<Fornecedor[]>();
  readonly atualizar = output<void>();
  readonly administrador = input(false);
  readonly erro = signal('');
  readonly salvando = signal(false);
  readonly editando = signal<number | null>(null);
  readonly formularioAberto = signal(false);
  form = { nome: '', contato: '', email: '', telefone: '' };
  constructor(private readonly api: EstoqueApiService) {}

  abrir(fornecedor?: Fornecedor): void {
    this.editando.set(fornecedor?.id ?? null);
    this.form = { nome: fornecedor?.nome ?? '', contato: fornecedor?.contato ?? '', email: fornecedor?.email ?? '', telefone: fornecedor?.telefone ?? '' };
    this.erro.set('');
    this.formularioAberto.set(true);
  }
  async salvar(): Promise<void> {
    this.salvando.set(true); this.erro.set('');
    try {
      const id = this.editando();
      if (id) await this.api.atualizarFornecedor(id, this.form);
      else await this.api.criarFornecedor(this.form);
      this.formularioAberto.set(false);
      this.atualizar.emit();
    } catch (error) {
      this.erro.set(error instanceof HttpErrorResponse ? error.error?.erro ?? 'Falha ao salvar fornecedor.' : 'Falha ao salvar fornecedor.');
    } finally { this.salvando.set(false); }
  }
  async alternar(f: Fornecedor): Promise<void> {
    this.erro.set('');
    try { await this.api.definirFornecedorAtivo(f.id, !f.ativo); this.atualizar.emit(); }
    catch (error) { this.erro.set(error instanceof HttpErrorResponse ? error.error?.erro ?? 'Falha ao alterar fornecedor.' : 'Falha ao alterar fornecedor.'); }
  }
}
