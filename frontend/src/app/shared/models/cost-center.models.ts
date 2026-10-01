export interface CentroCusto { id: number; nome: string; ativo: boolean; }
export interface ConsumoCentroCusto {
  centroCustoId: number | null; centroCusto: string; produtoId: number; produto: string;
  solicitado: number; entregue: number;
}
