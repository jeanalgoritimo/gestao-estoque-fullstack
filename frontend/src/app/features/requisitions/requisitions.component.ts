import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EstoqueApiService } from '../../core/api/estoque-api.service';
import { CentroCusto } from '../../shared/models/cost-center.models';
import { Produto } from '../../shared/models/stock.models';
import { RequisicaoMaterial, EntregaMaterial, FiltroRequisicoes } from '../../shared/models/requisition.models';

@Component({ selector: 'app-requisitions', imports: [CommonModule, FormsModule], templateUrl: './requisitions.component.html' })
export class RequisitionsComponent implements OnInit {
  private readonly api = inject(EstoqueApiService);
  readonly centrosCusto = input<CentroCusto[]>([]);
  centroCustoId: number | null = null;
  filtroCentro = 'todos';
  readonly produtos = input<Produto[]>([]);
  readonly administrador = input(false);
  readonly podeEntregar = input(false);
  readonly usuarioId = input(0);
  readonly alterado = output<void>();
  readonly requisicoes = signal<RequisicaoMaterial[]>([]);
  readonly processando = signal(false);
  readonly carregando = signal(false);
  readonly erro = signal('');
  readonly sucesso = signal('');
  readonly nova = signal(false);
  readonly historicoId = signal<number | null>(null);
  readonly entregas = signal<EntregaMaterial[]>([]);
  readonly carregandoHistorico = signal(false);
  readonly ativos = computed(() => this.produtos().filter(p => p.ativo));
  editandoId: number | null = null;
  versaoEdicao = '';
  finalidade = '';
  produtoId: number | null = null;
  quantidade: number | null = null;
  itens: { produtoId: number; quantidade: number }[] = [];
  quantidades: Record<string, number> = {};
  cancelandoId: number | null = null;
  motivo = '';
  busca = '';
  filtro = 0;
  solicitante = ''; inicio = ''; fim = ''; tamanhoPagina = 10;
  readonly pagina = signal(1);
  readonly total = signal(0);
  readonly totalPaginas = signal(1);
  private consultaAtual = 0;
  private filtrosAplicados: FiltroRequisicoes = { busca: '', solicitante: '', situacao: 0, centroCustoId: null, semCentro: false, inicio: '', fim: '', tamanhoPagina: 10 };
  ngOnInit(): void { void this.recarregar(); }
  status(r: RequisicaoMaterial): string {
    return ['', 'Rascunho', 'Pendente', 'Aprovada', 'Parcialmente atendida', 'Atendida', 'Cancelada'][r.situacao];
  }
  async aplicarFiltros(): Promise<void> {
    if (this.inicio && this.fim && this.fim < this.inicio) { this.erro.set('A data final deve ser igual ou posterior à inicial.'); return; }
    this.filtrosAplicados = { busca: this.busca.trim(), solicitante: this.solicitante.trim(), situacao: this.filtro,
      centroCustoId: this.filtroCentro === 'todos' || this.filtroCentro === 'sem' ? null : Number(this.filtroCentro),
      semCentro: this.filtroCentro === 'sem', inicio: this.inicio, fim: this.fim, tamanhoPagina: this.tamanhoPagina };
    this.pagina.set(1); await this.recarregar();
  }
  limparFiltros(): void {
    this.busca = ''; this.solicitante = ''; this.filtro = 0; this.filtroCentro = 'todos'; this.inicio = ''; this.fim = ''; this.tamanhoPagina = 10;
    void this.aplicarFiltros();
  }
  mudarPagina(pagina: number): void {
    if (this.carregando() || this.processando() || pagina < 1 || pagina > this.totalPaginas()) return;
    this.pagina.set(pagina); void this.recarregar();
  }
  podeEnviar(r: RequisicaoMaterial): boolean {
    return r.situacao === 1 && (this.administrador() || r.solicitanteId === this.usuarioId());
  }
  podeCancelar(r: RequisicaoMaterial): boolean {
    return r.situacao < 5 && (this.administrador() ||
      (r.solicitanteId === this.usuarioId() && r.situacao <= 2));
  }
  nomeProduto(id: number): string { return this.produtos().find(p => p.id === id)?.nome ?? `#${id}`; }
  adicionar(): void {
    if (!this.produtoId || !Number.isInteger(this.quantidade) || this.quantidade! <= 0 || this.quantidade! > 2147483647 ||
      this.itens.length >= 100 || this.itens.some(i => i.produtoId === this.produtoId)) {
      this.erro.set('Selecione um produto distinto e uma quantidade inteira positiva.'); return;
    }
    this.itens = [...this.itens, { produtoId: this.produtoId, quantidade: this.quantidade! }];
    this.produtoId = null; this.quantidade = null; this.erro.set('');
  }
  remover(id: number): void { this.itens = this.itens.filter(i => i.produtoId !== id); }
  editar(r: RequisicaoMaterial): void {
    if (!this.podeEnviar(r) || this.processando()) return;
    this.centroCustoId = r.centroCustoId;
    this.editandoId = r.id; this.versaoEdicao = r.versao; this.finalidade = r.finalidade;
    this.itens = r.itens.map(i => ({ produtoId: i.produtoId, quantidade: i.quantidade }));
    this.produtoId = null; this.quantidade = null; this.erro.set(''); this.sucesso.set(''); this.nova.set(true);
  }
  fecharEdicao(): void {
    if (this.processando()) return;
    this.nova.set(false); this.editandoId = null; this.versaoEdicao = '';
  }
  abrirNova(): void {
    this.centroCustoId = null;
    this.editandoId = null; this.versaoEdicao = '';
    this.finalidade = ''; this.itens = []; this.produtoId = null; this.quantidade = null;
    this.erro.set(''); this.sucesso.set(''); this.nova.set(true);
  }
  private mensagem(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) return 'Não foi possível conectar à API.';
      if (error.status === 401) return 'Sessão expirada. Entre novamente.';
      if (error.status === 403) return 'Seu perfil não permite esta operação.';
      return error.error?.erro ?? error.error?.title ?? `Falha na requisição (${error.status}).`;
    }
    return 'Não foi possível concluir a operação.';
  }
  async recarregar(): Promise<void> {
    const consulta = ++this.consultaAtual;
    this.carregando.set(true); this.erro.set('');
    this.requisicoes.set([]); this.historicoId.set(null); this.cancelandoId = null;
    try {
      const resultado = await this.api.requisicoes(this.filtrosAplicados, this.pagina());
      if (consulta !== this.consultaAtual) return;
      this.requisicoes.set(resultado.itens); this.pagina.set(resultado.pagina);
      this.total.set(resultado.total); this.totalPaginas.set(resultado.totalPaginas);
    }
    catch (error) { if (consulta === this.consultaAtual) this.erro.set(this.mensagem(error)); }
    finally { if (consulta === this.consultaAtual) this.carregando.set(false); }
  }
  private async executar(acao: () => Promise<unknown>, mensagem: string): Promise<void> {
    if (this.processando()) return;
    this.processando.set(true); this.erro.set(''); this.sucesso.set('');
    try { await acao(); this.sucesso.set(mensagem); await this.recarregar(); this.alterado.emit(); }
    catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.processando.set(false); }
  }
  async criar(): Promise<void> {
    if (!this.centroCustoId || !this.centrosCusto().some(c => c.id === this.centroCustoId && c.ativo) || !this.finalidade.trim() || !this.itens.length || this.itens.some(i =>
      !Number.isInteger(i.quantidade) || i.quantidade <= 0 || i.quantidade > 2147483647)) {
      this.erro.set('Selecione um centro de custo ativo, informe finalidade, itens e quantidades inteiras positivas.'); return;
    }
    const id = this.editandoId;
    await this.executar(async () => {
      if (id !== null) await this.api.editarRequisicao(id, { centroCustoId: this.centroCustoId!, finalidade: this.finalidade, itens: this.itens, versao: this.versaoEdicao });
      else await this.api.criarRequisicao({ centroCustoId: this.centroCustoId!, finalidade: this.finalidade, itens: this.itens });
      this.nova.set(false); this.editandoId = null; this.versaoEdicao = '';
    }, id !== null ? 'Rascunho atualizado.' : 'Rascunho criado. Envie a requisição para aprovação.');
  }
  enviar(r: RequisicaoMaterial): Promise<void> {
    return this.executar(() => this.api.enviarRequisicao(r.id), 'Requisição enviada para aprovação.');
  }
  aprovar(r: RequisicaoMaterial): Promise<void> {
    return this.executar(() => this.api.aprovarRequisicao(r.id), 'Requisição aprovada e estoque reservado.');
  }
  async cancelar(r: RequisicaoMaterial): Promise<void> {
    if (!this.motivo.trim()) { this.erro.set('Informe o motivo do cancelamento.'); return; }
    await this.executar(async () => {
      await this.api.cancelarRequisicao(r.id, this.motivo); this.cancelandoId = null; this.motivo = '';
    }, 'Requisição cancelada. Reservas pendentes liberadas; entregas anteriores mantidas.');
  }
  async entregar(r: RequisicaoMaterial): Promise<void> {
    const itens = r.itens.map(i => ({ produtoId: i.produtoId, quantidade: this.quantidades[`${r.id}-${i.produtoId}`] ?? 0 }))
      .filter(i => i.quantidade !== 0);
    if (!itens.length || itens.some(i => !Number.isInteger(i.quantidade) || i.quantidade <= 0 ||
      i.quantidade > r.itens.find(l => l.produtoId === i.produtoId)!.quantidade - r.itens.find(l => l.produtoId === i.produtoId)!.quantidadeEntregue)) {
      this.erro.set('Informe quantidades inteiras positivas dentro do saldo pendente.'); return;
    }
    await this.executar(async () => {
      await this.api.entregarRequisicao(r.id, itens);
      for (const i of r.itens) delete this.quantidades[`${r.id}-${i.produtoId}`];
      if (this.historicoId() === r.id) this.historicoId.set(null);
    }, 'Entrega registrada no histórico de estoque.');
  }
  async verEntregas(id: number): Promise<void> {
    if (this.historicoId() === id) { this.historicoId.set(null); return; }
    this.historicoId.set(id); this.entregas.set([]); this.carregandoHistorico.set(true);
    try { const linhas = await this.api.entregasRequisicao(id); if (this.historicoId() === id) this.entregas.set(linhas); }
    catch (error) { this.erro.set(this.mensagem(error)); }
    finally { this.carregandoHistorico.set(false); }
  }
}
