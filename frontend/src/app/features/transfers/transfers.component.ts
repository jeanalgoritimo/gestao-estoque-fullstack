import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { Produto } from '../../shared/models/stock.models';
import { PosicaoEstoque } from '../../shared/models/location.models';
import { HistoricoTransferencias, SaldosProduto } from '../../shared/models/transfer.models';
import { quantidadeValida } from '../../shared/models/quantity';

@Component({ selector: 'app-transfers', imports: [CommonModule, FormsModule], templateUrl: './transfers.component.html' })
export class TransfersComponent {
  private readonly api = inject(EstoqueApiService);
  readonly produtos = input.required<Produto[]>();
  readonly posicoes = input.required<PosicaoEstoque[]>();
  readonly podeMovimentar = input(false);
  readonly alterado = output<void>();
  readonly saldos = signal<SaldosProduto | null>(null);
  readonly historico = signal<HistoricoTransferencias | null>(null);
  readonly carregando = signal(false);
  readonly processando = signal(false);
  readonly erro = signal('');
  readonly sucesso = signal('');
  readonly destinos = computed(() => this.posicoes().filter(p => p.ativo && p.almoxarifadoAtivo));
  produtoId: number | null = null;
  origem = '';
  destinoId: number | null = null;
  quantidade: number | null = null;
  motivo = '';
  private consulta = 0;
  private readonly formato = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 });
  formatar(q: number): string { return this.formato.format(q); }
  chave(id: number | null): string { return id === null ? 'sem' : String(id); }
  origemId(): number | null { return this.origem === 'sem' ? null : Number(this.origem); }
  disponivel(): number { return this.saldos()?.locais.find(l => this.chave(l.posicaoEstoqueId) === this.origem)?.disponivel ?? 0; }
  private mensagem(e: unknown): string {
    return e instanceof HttpErrorResponse ? e.error?.erro ?? 'Não foi possível concluir. Recarregue os dados.' : 'Não foi possível concluir.';
  }
  async selecionar(): Promise<void> {
    this.saldos.set(null); this.historico.set(null); this.origem = ''; this.destinoId = null; this.quantidade = null; this.motivo = ''; this.sucesso.set('');
    await this.carregar();
  }
  async carregar(pagina = 1): Promise<void> {
    const id = this.produtoId; const consulta = ++this.consulta;
    if (!id) { this.carregando.set(false); return; }
    this.carregando.set(true); this.erro.set('');
    try {
      const [saldos, historico] = await Promise.all([this.api.saldosLocais(id), this.api.transferencias(id, pagina)]);
      if (consulta !== this.consulta) return;
      this.saldos.set(saldos); this.historico.set(historico);
      if (!this.origem) this.origem = this.chave(saldos.locais.find(l => l.disponivel > 0)?.posicaoEstoqueId ?? null);
    } catch (e) { if (consulta === this.consulta) { this.erro.set(this.mensagem(e)); this.saldos.set(null); this.historico.set(null); } }
    finally { if (consulta === this.consulta) this.carregando.set(false); }
  }
  async transferir(): Promise<void> {
    if (this.processando() || this.carregando() || !this.podeMovimentar()) return;
    const saldos = this.saldos(); const produto = this.produtos().find(p => p.id === this.produtoId);
    if (!saldos || !produto?.ativo || !this.origem || !this.destinoId || this.origemId() === this.destinoId ||
        !this.destinos().some(p => p.id === this.destinoId) || !quantidadeValida(this.quantidade) || this.quantidade <= 0 ||
        this.quantidade > this.disponivel() || !this.motivo.trim() || this.motivo.trim().length > 150) {
      this.erro.set('Informe locais distintos, quantidade positiva dentro do saldo disponível e motivo de até 150 caracteres.'); return;
    }
    this.processando.set(true); this.erro.set(''); this.sucesso.set('');
    try {
      await this.api.transferirEstoque({ produtoId: saldos.id, origemId: this.origemId(), destinoId: this.destinoId,
        quantidade: this.quantidade, motivo: this.motivo.trim(), versaoProduto: saldos.versaoProduto });
      this.quantidade = null; this.motivo = ''; this.sucesso.set('Transferência registrada. O saldo total foi preservado.');
      this.alterado.emit(); await this.carregar();
    } catch (e) { this.erro.set(this.mensagem(e)); }
    finally { this.processando.set(false); }
  }
}
