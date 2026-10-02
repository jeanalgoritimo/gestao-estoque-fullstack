export interface SaldoPorLocal { posicaoEstoqueId: number | null; local: string; quantidade: number; reservado: number; disponivel: number; }
export interface SaldosProduto { id: number; nome: string; unidade: string; estoque: number; estoqueReservado: number; estoqueDisponivel: number; versaoProduto: string; locais: SaldoPorLocal[]; }
export interface Transferencia { id: number; produtoId: number; origemId: number | null; destinoId: number; origemDescricao: string; destinoDescricao: string; quantidade: number; dataUtc: string; usuarioNome: string; motivo: string; }
export interface HistoricoTransferencias { itens: Transferencia[]; total: number; pagina: number; totalPaginas: number; }
export interface TransferenciaInput { produtoId: number; origemId: number | null; destinoId: number; quantidade: number; motivo: string; versaoProduto: string; }
