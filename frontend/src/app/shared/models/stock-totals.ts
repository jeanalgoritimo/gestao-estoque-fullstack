import { Produto } from './stock.models';
export function agruparSaldos(produtos: Produto[]): { unidade: string; quantidade: number }[] {
  const totais = new Map<string, number>();
  for (const p of produtos) totais.set(p.unidade, (totais.get(p.unidade) ?? 0) + p.estoque);
  return [...totais].map(([unidade, quantidade]) => ({ unidade, quantidade })).sort((a, b) => a.unidade.localeCompare(b.unidade));
}
