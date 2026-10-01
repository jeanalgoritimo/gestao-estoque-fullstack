import { Component, input, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { CentroCusto, ConsumoCentroCusto } from '../../shared/models/cost-center.models';
@Component({ selector: 'app-consumption', imports: [FormsModule], templateUrl: './consumption.component.html' })
export class ConsumptionComponent implements OnInit {
  readonly centros = input<CentroCusto[]>([]);
  readonly linhas = signal<ConsumoCentroCusto[]>([]);
  readonly carregando = signal(false);
  readonly erro = signal('');
  aplicado = '';
  private readonly hoje = new Date();
  inicio = `${this.hoje.getFullYear()}-${String(this.hoje.getMonth() + 1).padStart(2, '0')}-01`;
  fim = `${this.hoje.getFullYear()}-${String(this.hoje.getMonth() + 1).padStart(2, '0')}-${String(this.hoje.getDate()).padStart(2, '0')}`;
  centro = 'todos';
  constructor(private readonly api: EstoqueApiService) {}
  ngOnInit(): void { void this.consultar(); }
  async consultar(): Promise<void> {
    if (this.carregando()) return;
    if (!this.inicio || !this.fim || this.fim < this.inicio) { this.erro.set('Informe um período válido.'); return; }
    this.carregando.set(true); this.erro.set(''); this.linhas.set([]); this.aplicado = '';
    try { this.linhas.set(await this.api.consumoCentros(this.inicio, this.fim, this.centro === 'todos' || this.centro === 'sem' ? null : Number(this.centro), this.centro === 'sem'));
      const nome = this.centro === 'todos' ? 'Todos os centros' : this.centro === 'sem' ? 'Sem centro de custo' : this.centros().find(c => c.id === Number(this.centro))?.nome ?? '';
      this.aplicado = `${this.inicio} a ${this.fim} · ${nome}`;
    } catch (e) { this.erro.set(e instanceof HttpErrorResponse ? e.error?.erro ?? 'Falha ao consultar consumo.' : 'Falha ao consultar consumo.'); }
    finally { this.carregando.set(false); }
  }
  exportar(): void {
    const celula = (v: string | number) => { let s = String(v); if (/^\s*[=+\-@]/.test(s)) s = "'" + s; return '"' + s.replace(/"/g, '""') + '"'; };
    const rows = [['Centro de custo', 'Código produto', 'Produto', 'Solicitado (data de criação)', 'Entregue (data da saída)', 'Devolvido (data da entrada)', 'Consumo líquido no período'],
      ...this.linhas().map(l => [l.centroCusto, l.produtoId, l.produto, l.solicitado, l.entregue, l.devolvido, l.consumoLiquido])];
    const url = URL.createObjectURL(new Blob(['\ufeff' + rows.map(r => r.map(celula).join(';')).join('\r\n')], { type: 'text/csv;charset=utf-8' }));
    const a = document.createElement('a'); a.href = url; a.download = 'consumo-centros-custo.csv'; a.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}
