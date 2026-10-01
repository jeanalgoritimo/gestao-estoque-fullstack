import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { Almoxarifado, PosicaoEstoque } from '../../shared/models/location.models';

@Component({ selector: 'app-locations', imports: [FormsModule], templateUrl: './locations.component.html' })
export class LocationsComponent {
  private readonly api = inject(EstoqueApiService);
  readonly almoxarifados = input.required<Almoxarifado[]>();
  readonly posicoes = input.required<PosicaoEstoque[]>();
  readonly administrador = input(false);
  readonly alterado = output<void>();
  readonly processando = signal(false);
  readonly erro = signal('');
  readonly sucesso = signal('');
  novoAlmoxarifado = false; novaPosicao = false;
  almoxarifadoId: number | null = null; nome = '';
  posicaoId: number | null = null; almoxarifadoPosicao: number | null = null;
  corredor = ''; estante = ''; prateleira = '';
  editarAlmoxarifado(a?: Almoxarifado): void {
    this.almoxarifadoId = a?.id ?? null; this.nome = a?.nome ?? ''; this.novoAlmoxarifado = true; this.erro.set('');
  }
  editarPosicao(p?: PosicaoEstoque): void {
    this.posicaoId = p?.id ?? null; this.almoxarifadoPosicao = p?.almoxarifadoId ?? null;
    this.corredor = p?.corredor ?? ''; this.estante = p?.estante ?? ''; this.prateleira = p?.prateleira ?? '';
    this.novaPosicao = true; this.erro.set('');
  }
  private async executar(acao: () => Promise<unknown>): Promise<void> {
    if (this.processando() || !this.administrador()) return;
    this.processando.set(true); this.erro.set(''); this.sucesso.set('');
    try { await acao(); this.sucesso.set('Cadastro atualizado.'); this.alterado.emit(); }
    catch (error) { this.erro.set(error instanceof HttpErrorResponse ? error.error?.erro ?? `Falha (${error.status}).` : 'Não foi possível concluir.'); }
    finally { this.processando.set(false); }
  }
  async salvarAlmoxarifado(): Promise<void> {
    if (!this.nome.trim()) { this.erro.set('Informe o nome do almoxarifado.'); return; }
    await this.executar(async () => {
      await this.api.salvarAlmoxarifado(this.almoxarifadoId, this.nome); this.novoAlmoxarifado = false;
    });
  }
  async salvarPosicao(): Promise<void> {
    if (!this.almoxarifadoPosicao || !this.corredor.trim() || !this.estante.trim() || !this.prateleira.trim()) {
      this.erro.set('Informe almoxarifado, corredor, estante e prateleira.'); return;
    }
    await this.executar(async () => {
      await this.api.salvarPosicao(this.posicaoId, { almoxarifadoId: this.almoxarifadoPosicao!, corredor: this.corredor, estante: this.estante, prateleira: this.prateleira }); this.novaPosicao = false;
    });
  }
  alternarAlmoxarifado(a: Almoxarifado): Promise<void> { return this.executar(() => this.api.ativarAlmoxarifado(a.id, !a.ativo)); }
  alternarPosicao(p: PosicaoEstoque): Promise<void> { return this.executar(() => this.api.ativarPosicao(p.id, !p.ativo)); }
}
