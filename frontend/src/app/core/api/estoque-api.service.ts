import { UnidadeMedida } from '../../shared/models/unit.models';
import { Almoxarifado, PosicaoEstoque, PosicaoInput } from '../../shared/models/location.models';
import { CentroCusto, ConsumoCentroCusto } from '../../shared/models/cost-center.models';
import { RequisicaoMaterial, EntregaMaterial, DevolucaoMaterial, FiltroRequisicoes, PaginaRequisicoes } from '../../shared/models/requisition.models';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  Categoria,
  Fornecedor,
  InventarioFisico,
  Movimento,
  PedidoCompra,
  RecebimentoPedidoCompra,
  NovoPedidoCompra,
  SugestaoReposicao,
  Perfil,
  Produto,
  ProdutoForm,
  TipoMovimento,
  Usuario,
} from '../../shared/models/stock.models';

export interface MovimentoInput {
  tipo: TipoMovimento;
  quantidade: number;
  observacao: string | null;
  documentoOrigem: string;
  motivo: string;
  dataEfetivaUtc: string | null;
  custoUnitario: number | null;
}

@Injectable({ providedIn: 'root' })
export class EstoqueApiService {
  private readonly http = inject(HttpClient);

  centrosCusto() { return firstValueFrom(this.http.get<CentroCusto[]>('/api/centros-custo')); }
  criarCentroCusto(nome: string) { return firstValueFrom(this.http.post<CentroCusto>('/api/centros-custo', { nome })); }
  editarCentroCusto(id: number, nome: string) { return firstValueFrom(this.http.put<CentroCusto>(`/api/centros-custo/${id}`, { nome })); }
  definirCentroCustoAtivo(id: number, ativo: boolean) { return firstValueFrom(this.http.patch<CentroCusto>(`/api/centros-custo/${id}/ativo`, ativo)); }
  consumoCentros(inicio: string, fim: string, centroCustoId: number | null, semCentro: boolean) {
    const params: Record<string, string> = { inicio, fim, semCentro: String(semCentro) };
    if (centroCustoId !== null) params['centroCustoId'] = String(centroCustoId);
    return firstValueFrom(this.http.get<ConsumoCentroCusto[]>('/api/relatorios/consumo', { params }));
  }
  requisicoes(filtro: FiltroRequisicoes, pagina = 1) {
    const params: Record<string, string> = { pagina: String(pagina), tamanhoPagina: String(filtro.tamanhoPagina), semCentro: String(filtro.semCentro) };
    if (filtro.busca) params['busca'] = filtro.busca;
    if (filtro.solicitante) params['solicitante'] = filtro.solicitante;
    if (filtro.situacao) params['situacao'] = String(filtro.situacao);
    if (filtro.centroCustoId !== null) params['centroCustoId'] = String(filtro.centroCustoId);
    if (filtro.inicio) params['inicio'] = filtro.inicio;
    if (filtro.fim) params['fim'] = filtro.fim;
    return firstValueFrom(this.http.get<PaginaRequisicoes>('/api/requisicoes', { params }));
  }
  criarRequisicao(input: { centroCustoId: number; finalidade: string; itens: { produtoId: number; quantidade: number }[] }) {
    return firstValueFrom(this.http.post<{ id: number }>('/api/requisicoes', input));
  }
  editarRequisicao(id: number, input: { centroCustoId: number; finalidade: string; itens: { produtoId: number; quantidade: number }[]; versao: string }) {
    return firstValueFrom(this.http.put<void>(`/api/requisicoes/${id}`, input));
  }
  enviarRequisicao(id: number) { return firstValueFrom(this.http.post<void>(`/api/requisicoes/${id}/enviar`, {})); }
  aprovarRequisicao(id: number) { return firstValueFrom(this.http.post<void>(`/api/requisicoes/${id}/aprovar`, {})); }
  cancelarRequisicao(id: number, motivo: string) {
    return firstValueFrom(this.http.post<void>(`/api/requisicoes/${id}/cancelar`, { motivo }));
  }
  entregarRequisicao(id: number, itens: { produtoId: number; quantidade: number }[]) {
    return firstValueFrom(this.http.post<void>(`/api/requisicoes/${id}/entregar`, { itens }));
  }
  devolverRequisicao(id: number, itens: { produtoId: number; quantidade: number }[], motivo: string) {
    return firstValueFrom(this.http.post<void>(`/api/requisicoes/${id}/devolver`, { itens, motivo }));
  }
  devolucoesRequisicao(id: number) {
    return firstValueFrom(this.http.get<DevolucaoMaterial[]>(`/api/requisicoes/${id}/devolucoes`));
  }
  entregasRequisicao(id: number) {
    return firstValueFrom(this.http.get<EntregaMaterial[]>(`/api/requisicoes/${id}/entregas`));
  }
  produtos() {
    return firstValueFrom(this.http.get<Produto[]>('/api/produtos'));
  }
  fornecedores() {
    return firstValueFrom(this.http.get<Fornecedor[]>('/api/fornecedores'));
  }
  criarFornecedor(form: { nome: string; contato: string; email: string; telefone: string }) {
    return firstValueFrom(this.http.post<Fornecedor>('/api/fornecedores', form));
  }
  atualizarFornecedor(id: number, form: { nome: string; contato: string; email: string; telefone: string }) {
    return firstValueFrom(this.http.put<Fornecedor>(`/api/fornecedores/${id}`, form));
  }
  definirFornecedorAtivo(id: number, ativo: boolean) {
    return firstValueFrom(this.http.patch<Fornecedor>(`/api/fornecedores/${id}/ativo`, ativo));
  }
  categorias() {
    return firstValueFrom(this.http.get<Categoria[]>('/api/categorias'));
  }
  perfis() {
    return firstValueFrom(this.http.get<Perfil[]>('/api/perfis'));
  }
  usuarios() {
    return firstValueFrom(this.http.get<Usuario[]>('/api/usuarios'));
  }
  movimentos(produtoId: number) {
    return firstValueFrom(this.http.get<Movimento[]>(`/api/produtos/${produtoId}/movimentos`));
  }

  criarProduto(form: ProdutoForm) {
    return firstValueFrom(this.http.post<Produto>('/api/produtos', form));
  }
  atualizarProduto(id: number, form: ProdutoForm) {
    return firstValueFrom(this.http.put<Produto>(`/api/produtos/${id}`, form));
  }
  desativarProduto(id: number) {
    return firstValueFrom(this.http.delete<void>(`/api/produtos/${id}`));
  }
  criarMovimento(id: number, input: MovimentoInput) {
    return firstValueFrom(this.http.post<Movimento>(`/api/produtos/${id}/movimentos`, input));
  }

  criarCategoria(nome: string) {
    return firstValueFrom(this.http.post<Categoria>('/api/categorias', { nome }));
  }
  atualizarCategoria(id: number, nome: string) {
    return firstValueFrom(this.http.put<Categoria>(`/api/categorias/${id}`, { nome }));
  }
  definirCategoriaAtiva(id: number, ativo: boolean) {
    return firstValueFrom(this.http.patch(`/api/categorias/${id}/ativo`, ativo));
  }

  criarPerfil(form: Omit<Perfil, 'id' | 'sistema' | 'ativo'>) {
    return firstValueFrom(this.http.post<Perfil>('/api/perfis', form));
  }
  atualizarPerfil(id: number, form: Omit<Perfil, 'id' | 'sistema' | 'ativo'>) {
    return firstValueFrom(this.http.put<Perfil>(`/api/perfis/${id}`, form));
  }
  definirPerfilAtivo(id: number, ativo: boolean) {
    return firstValueFrom(this.http.patch(`/api/perfis/${id}/ativo`, ativo));
  }

  criarUsuario(form: { nome: string; email: string; senha: string; perfilId: number }) {
    return firstValueFrom(this.http.post<Usuario>('/api/usuarios', form));
  }
  definirUsuarioAtivo(id: number, ativo: boolean) {
    return firstValueFrom(this.http.patch(`/api/usuarios/${id}/ativo`, ativo));
  }
  alterarPerfilUsuario(id: number, perfilId: number) {
    return firstValueFrom(this.http.put(`/api/usuarios/${id}/perfil`, perfilId));
  }

  pedidosCompra() {
    return firstValueFrom(this.http.get<PedidoCompra[]>('/api/pedidos-compra'));
  }
  recebimentosPedidoCompra(id: number) {
    return firstValueFrom(this.http.get<RecebimentoPedidoCompra[]>(`/api/pedidos-compra/${id}/recebimentos`));
  }
  unidadesMedida() { return firstValueFrom(this.http.get<UnidadeMedida[]>('/api/unidades-medida')); }
  salvarUnidade(id: number | null, sigla: string, nome: string) {
    return id === null ? firstValueFrom(this.http.post('/api/unidades-medida', { sigla, nome })) : firstValueFrom(this.http.put(`/api/unidades-medida/${id}`, { sigla, nome }));
  }
  ativarUnidade(id: number, ativo: boolean) { return firstValueFrom(this.http.patch(`/api/unidades-medida/${id}/ativo`, ativo)); }
  almoxarifados() { return firstValueFrom(this.http.get<Almoxarifado[]>('/api/localizacoes/almoxarifados')); }
  posicoes() { return firstValueFrom(this.http.get<PosicaoEstoque[]>('/api/localizacoes/posicoes')); }
  salvarAlmoxarifado(id: number | null, nome: string) {
    return id === null ? firstValueFrom(this.http.post('/api/localizacoes/almoxarifados', { nome })) : firstValueFrom(this.http.put(`/api/localizacoes/almoxarifados/${id}`, { nome }));
  }
  salvarPosicao(id: number | null, input: PosicaoInput) {
    return id === null ? firstValueFrom(this.http.post('/api/localizacoes/posicoes', input)) : firstValueFrom(this.http.put(`/api/localizacoes/posicoes/${id}`, input));
  }
  ativarAlmoxarifado(id: number, ativo: boolean) { return firstValueFrom(this.http.patch(`/api/localizacoes/almoxarifados/${id}/ativo`, ativo)); }
  ativarPosicao(id: number, ativo: boolean) { return firstValueFrom(this.http.patch(`/api/localizacoes/posicoes/${id}/ativo`, ativo)); }
  sugestoesReposicao() {
    return firstValueFrom(this.http.get<SugestaoReposicao[]>('/api/reposicao'));
  }
  editarRascunhoCompra(id: number, itens: { produtoId: number; quantidade: number }[], versao: string) {
    return firstValueFrom(this.http.put(`/api/pedidos-compra/${id}/rascunho`, { itens, versao }));
  }
  confirmarRascunhoCompra(id: number, versao: string) {
    return firstValueFrom(this.http.post(`/api/pedidos-compra/${id}/confirmar`, { versao }));
  }
  criarPedidoCompra(input: NovoPedidoCompra) {
    return firstValueFrom(this.http.post<{ id: number }>('/api/pedidos-compra', input));
  }
  receberPedidoCompra(id: number, itens: { produtoId: number; quantidade: number; custoUnitario: number }[]) {
    return firstValueFrom(this.http.post(`/api/pedidos-compra/${id}/receber`, { itens }));
  }
  cancelarPedidoCompra(id: number, motivo: string) {
    return firstValueFrom(this.http.post(`/api/pedidos-compra/${id}/cancelar`, { motivo }));
  }
  inventarios() {
    return firstValueFrom(this.http.get<InventarioFisico[]>('/api/inventarios'));
  }
  abrirInventario(produtoId: number) {
    return firstValueFrom(this.http.post<InventarioFisico>('/api/inventarios', { produtoId }));
  }
  registrarContagem(id: number, quantidade: number) {
    return firstValueFrom(
      this.http.put<InventarioFisico>(`/api/inventarios/${id}/contagem`, { quantidade }),
    );
  }
  confirmarInventario(id: number, motivo: string, quantidadeEsperada: number) {
    return firstValueFrom(
      this.http.post<InventarioFisico>(`/api/inventarios/${id}/confirmar`, {
        motivo,
        quantidadeEsperada,
      }),
    );
  }
  cancelarInventario(id: number) {
    return firstValueFrom(this.http.post(`/api/inventarios/${id}/cancelar`, {}));
  }
}
