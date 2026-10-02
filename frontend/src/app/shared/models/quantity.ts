export const QUANTIDADE_MAXIMA = 2147483647.999;
export function quantidadeValida(valor: unknown): valor is number {
  if (typeof valor !== 'number' || !Number.isFinite(valor) || valor < 0 || valor > QUANTIDADE_MAXIMA) return false;
  // A reconversão aceita milésimos representáveis sem aceitar uma quarta casa decimal.
  return Math.round(valor * 1000) / 1000 === valor;
}
export function diferencaQuantidade(a: number, b: number): number {
  return (Math.round(a * 1000) - Math.round(b * 1000)) / 1000;
}
export function somarQuantidade(a: number, b: number): number {
  return (Math.round(a * 1000) + Math.round(b * 1000)) / 1000;
}
