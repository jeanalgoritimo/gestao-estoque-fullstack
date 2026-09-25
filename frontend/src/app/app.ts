import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

type TipoMovimento = 1 | 2;
interface Produto { id: number; nome: string; categoria: string; preco: number; estoque: number; estoqueMinimo: number; ativo: boolean; estoqueBaixo: boolean; }
interface Movimento { id: number; produtoId: number; tipo: TipoMovimento; quantidade: number; dataUtc: string; observacao: string | null; }
interface ProdutoForm { nome: string; categoria: string; preco: number | null; estoqueMinimo: number | null; }

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly http = inject(HttpClient);
  readonly produtos = signal<Produto[]>([]);
  readonly movimentos = signal<Movimento[]>([]);
  readonly busca = signal('');
  readonly somenteAtivos = signal(true);
  readonly carregando = signal(false);
  readonly salvando = signal(false);
  readonly erro = signal('');
  readonly sucesso = signal('');
  readonly modal = signal<'produto' | 'movimento' | 'historico' | null>(null);
  readonly selecionado = signal<Produto | null>(null);
  readonly filtrados = computed(() => {
    const termo = this.busca().trim().toLocaleLowerCase('pt-BR');
    return this.produtos().filter(p => (!this.somenteAtivos() || p.ativo) &&
      (!termo || `${p.nome} ${p.categoria} ${p.id}`.toLocaleLowerCase('pt-BR').includes(termo)));
  });
  readonly ativos = computed(() => this.produtos().filter(p => p.ativo));
  readonly baixos = computed(() => this.ativos().filter(p => p.estoqueBaixo).length);
  readonly unidades = computed(() => this.ativos().reduce((total, p) => total + p.estoque, 0));
  readonly valorEstoque = computed(() => this.ativos().reduce((total, p) => total + p.estoque * p.preco, 0));

  form: ProdutoForm = this.formVazio();
  tipoMovimento: TipoMovimento = 1;
  quantidade: number | null = null;
  observacao = '';

  async ngOnInit(): Promise<void> { await this.recarregar(); }
  private formVazio(): ProdutoForm { return { nome: '', categoria: '', preco: null, estoqueMinimo: 5 }; }
  private mensagemErro(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) return 'Não foi possível conectar à API. Confira se ela está em execução na porta 5029.';
      return error.error?.erro ?? error.error?.title ?? `Falha na requisição (${error.status}).`;
    }
    return 'Não foi possível concluir a operação.';
  }
  async recarregar(): Promise<void> {
    this.carregando.set(true); this.erro.set('');
    try { this.produtos.set(await firstValueFrom(this.http.get<Produto[]>('/api/produtos'))); }
    catch (error) { this.erro.set(this.mensagemErro(error)); }
    finally { this.carregando.set(false); }
  }
  novoProduto(): void { this.selecionado.set(null); this.form = this.formVazio(); this.erro.set(''); this.modal.set('produto'); }
  editar(produto: Produto): void {
    this.selecionado.set(produto);
    this.form = { nome: produto.nome, categoria: produto.categoria, preco: produto.preco, estoqueMinimo: produto.estoqueMinimo };
    this.erro.set(''); this.modal.set('produto');
  }
  async salvarProduto(): Promise<void> {
    if (!this.form.nome.trim() || !this.form.categoria.trim() || this.form.preco === null || this.form.preco <= 0 ||
        this.form.estoqueMinimo === null || !Number.isInteger(this.form.estoqueMinimo) || this.form.estoqueMinimo < 0) {
      this.erro.set('Informe nome, categoria, preço maior que zero e estoque mínimo válido.'); return;
    }
    this.salvando.set(true); this.erro.set('');
    try {
      const id = this.selecionado()?.id;
      if (id) await firstValueFrom(this.http.put(`/api/produtos/${id}`, this.form));
      else await firstValueFrom(this.http.post('/api/produtos', this.form));
      this.modal.set(null); this.sucesso.set(id ? 'Produto atualizado.' : 'Produto cadastrado. Registre uma entrada para definir o saldo.');
      await this.recarregar();
    } catch (error) { this.erro.set(this.mensagemErro(error)); }
    finally { this.salvando.set(false); }
  }
  abrirMovimento(produto: Produto, tipo: TipoMovimento): void {
    this.selecionado.set(produto); this.tipoMovimento = tipo;
    this.quantidade = null; this.observacao = ''; this.erro.set(''); this.modal.set('movimento');
  }
  async salvarMovimento(): Promise<void> {
    const produto = this.selecionado();
    if (!produto || this.quantidade === null || !Number.isInteger(this.quantidade) || this.quantidade <= 0) {
      this.erro.set('Informe uma quantidade inteira maior que zero.'); return;
    }
    this.salvando.set(true); this.erro.set('');
    try {
      await firstValueFrom(this.http.post(`/api/produtos/${produto.id}/movimentos`, {
        tipo: this.tipoMovimento, quantidade: this.quantidade, observacao: this.observacao || null
      }));
      this.modal.set(null); this.sucesso.set('Movimentação registrada.'); await this.recarregar();
    } catch (error) { this.erro.set(this.mensagemErro(error)); }
    finally { this.salvando.set(false); }
  }
  async abrirHistorico(produto: Produto): Promise<void> {
    this.selecionado.set(produto); this.movimentos.set([]); this.erro.set(''); this.modal.set('historico');
    try { this.movimentos.set(await firstValueFrom(this.http.get<Movimento[]>(`/api/produtos/${produto.id}/movimentos`))); }
    catch (error) { this.erro.set(this.mensagemErro(error)); }
  }
  async desativar(produto: Produto): Promise<void> {
    if (!confirm(`Desativar ${produto.nome}? O histórico será mantido.`)) return;
    this.erro.set('');
    try {
      await firstValueFrom(this.http.delete(`/api/produtos/${produto.id}`));
      this.sucesso.set('Produto desativado.'); await this.recarregar();
    } catch (error) { this.erro.set(this.mensagemErro(error)); }
  }
  fechar(): void { if (!this.salvando()) { this.modal.set(null); this.erro.set(''); } }
  moeda(valor: number): string { return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor); }
}
