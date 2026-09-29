export type TipoMovimento = 1 | 2;
export interface Produto {
  id: number;
  nome: string;
  categoriaId: number;
  categoria: string;
  preco: number;
  estoque: number;
  estoqueMinimo: number;
  ativo: boolean;
  estoqueBaixo: boolean;
}
export interface Categoria {
  id: number;
  nome: string;
  ativo: boolean;
}
export interface Movimento {
  id: number;
  produtoId: number;
  tipo: TipoMovimento;
  quantidade: number;
  dataUtc: string;
  dataEfetivaUtc: string;
  documentoOrigem: string;
  motivo: string;
  usuarioNome: string;
  custoUnitario: number | null;
  saldoApos: number | null;
  observacao: string | null;
}
export interface ProdutoForm {
  nome: string;
  categoriaId: number | null;
  preco: number | null;
  estoqueMinimo: number | null;
}
export interface Usuario {
  id: number;
  nome: string;
  email: string;
  perfilId: number;
  perfil: string;
  ativo: boolean;
}
export interface Perfil {
  id: number;
  nome: string;
  sistema: boolean;
  ativo: boolean;
  cadastrarProdutos: boolean;
  gerenciarProdutos: boolean;
  cadastrarCategorias: boolean;
  gerenciarCategorias: boolean;
  movimentarEstoque: boolean;
}
