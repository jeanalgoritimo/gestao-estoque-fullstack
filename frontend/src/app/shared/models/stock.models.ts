export type TipoMovimento = 1 | 2;
export interface Produto {
  id: number;
  nome: string;
  categoriaId: number;
  categoria: string;
  fornecedorId: number | null;
  fornecedor: string | null;
  preco: number;
  estoque: number;
  estoqueMinimo: number;
  ativo: boolean;
  estoqueBaixo: boolean;
}
export interface Fornecedor {
  id: number;
  nome: string;
  contato: string | null;
  email: string | null;
  telefone: string | null;
  ativo: boolean;
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
export interface InventarioFisico {
  id: number;
  produtoId: number;
  produto: string;
  saldoInicial: number;
  quantidadeContada: number | null;
  diferenca: number | null;
  situacao: 1 | 2 | 3;
  abertoUtc: string;
  abertoPorNome: string;
  encerradoUtc: string | null;
  encerradoPorNome: string | null;
  motivo: string | null;
}
export interface ProdutoForm {
  fornecedorId: number | null;
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

export interface PedidoCompra {
  id: number;
  fornecedorId: number;
  fornecedor: string;
  situacao: 1 | 2 | 3 | 4 | 5;
  motivoCancelamento: string | null;
  criadoUtc: string;
  criadoPorNome: string;
  encerradoUtc: string | null;
  encerradoPorNome: string | null;
  itens: { produtoId: number; produto: string; quantidade: number; quantidadeRecebida: number }[];
}
export interface NovoPedidoCompra {
  fornecedorId: number;
  itens: { produtoId: number; quantidade: number }[];
}
