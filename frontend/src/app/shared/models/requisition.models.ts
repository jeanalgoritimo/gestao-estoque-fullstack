export interface RequisicaoMaterial {
  centroCustoId: number | null; centroCusto: string;
  versao: string;
  id: number; finalidade: string; solicitanteId: number; solicitanteNome: string;
  criadoUtc: string; situacao: 1 | 2 | 3 | 4 | 5 | 6;
  aprovadoUtc: string | null; aprovadoPorNome: string | null;
  encerradoUtc: string | null; encerradoPorNome: string | null; motivoCancelamento: string | null;
  itens: { produtoId: number; produto: string; quantidade: number; quantidadeEntregue: number }[];
}
export interface EntregaMaterial {
  id: number; entregaId: string; produtoId: number; produto: string; quantidade: number;
  dataUtc: string; usuarioNome: string; saldoApos: number;
}
