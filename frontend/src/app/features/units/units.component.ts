import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { UnidadeMedida } from '../../shared/models/unit.models';
@Component({ selector: 'app-units', imports: [FormsModule], templateUrl: './units.component.html' })
export class UnitsComponent {
  private readonly api = inject(EstoqueApiService);
  readonly unidades = input.required<UnidadeMedida[]>();
  readonly administrador = input(false);
  readonly alterado = output<void>();
  readonly processando = signal(false); readonly erro = signal(''); readonly sucesso = signal('');
  aberta = false; id: number | null = null; sigla = ''; nome = '';
  editar(u?: UnidadeMedida): void { this.id = u?.id ?? null; this.sigla = u?.sigla ?? ''; this.nome = u?.nome ?? ''; this.aberta = true; this.erro.set(''); }
  private async executar(acao: () => Promise<unknown>): Promise<void> {
    if (this.processando() || !this.administrador()) return;
    this.processando.set(true); this.erro.set(''); this.sucesso.set('');
    try { await acao(); this.sucesso.set('Unidade atualizada.'); this.alterado.emit(); }
    catch (e) { this.erro.set(e instanceof HttpErrorResponse ? e.error?.erro ?? `Falha (${e.status}).` : 'Não foi possível concluir.'); }
    finally { this.processando.set(false); }
  }
  salvar(): Promise<void> { return this.executar(async () => { await this.api.salvarUnidade(this.id, this.sigla, this.nome); this.aberta = false; }); }
  alternar(u: UnidadeMedida): Promise<void> { return this.executar(() => this.api.ativarUnidade(u.id, !u.ativo)); }
}
