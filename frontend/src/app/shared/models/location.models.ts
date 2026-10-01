export interface Almoxarifado { id: number; nome: string; ativo: boolean; }
export interface PosicaoEstoque {
  id: number; almoxarifadoId: number; almoxarifado: string;
  corredor: string; estante: string; prateleira: string;
  ativo: boolean; almoxarifadoAtivo: boolean; descricao: string;
}
export interface PosicaoInput { almoxarifadoId: number; corredor: string; estante: string; prateleira: string; }
