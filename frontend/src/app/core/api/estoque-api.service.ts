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
  criarPedidoCompra(input: NovoPedidoCompra) {
    return firstValueFrom(this.http.post<{ id: number }>('/api/pedidos-compra', input));
  }
  receberPedidoCompra(id: number, itens: { produtoId: number; quantidade: number }[]) {
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
