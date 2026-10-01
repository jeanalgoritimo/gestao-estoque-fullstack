import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { CentroCusto } from '../../shared/models/cost-center.models';
@Component({ selector: 'app-cost-centers', imports: [FormsModule], templateUrl: './cost-centers.component.html' })
export class CostCentersComponent {
  readonly centros = input<CentroCusto[]>([]);
  readonly administrador = input(false);
  readonly alterado = output<void>();
  readonly processando = signal(false);
  readonly erro = signal('');
  aberto = false; id: number | null = null; nome = ''; busca = '';
  constructor(private readonly api: EstoqueApiService) {}
  filtrados(): CentroCusto[] { return this.centros().filter(c => c.nome.toLocaleLowerCase('pt-BR').includes(this.busca.trim().toLocaleLowerCase('pt-BR'))); }
  abrir(c?: CentroCusto): void { this.id = c?.id ?? null; this.nome = c?.nome ?? ''; this.aberto = true; this.erro.set(''); }
  private mensagem(e: unknown): string { return e instanceof HttpErrorResponse ? e.error?.erro ?? 'Não foi possível concluir a operação.' : 'Não foi possível concluir a operação.'; }
  async salvar(): Promise<void> {
    if (this.processando()) return;
    this.processando.set(true); this.erro.set('');
    try { if (this.id !== null) await this.api.editarCentroCusto(this.id, this.nome); else await this.api.criarCentroCusto(this.nome);
      this.aberto = false; this.alterado.emit(); }
    catch (e) { this.erro.set(this.mensagem(e)); } finally { this.processando.set(false); }
  }
  async alternar(c: CentroCusto): Promise<void> {
    if (this.processando()) return;
    this.processando.set(true); this.erro.set('');
    try { await this.api.definirCentroCustoAtivo(c.id, !c.ativo); this.alterado.emit(); }
    catch (e) { this.erro.set(this.mensagem(e)); } finally { this.processando.set(false); }
  }
}
