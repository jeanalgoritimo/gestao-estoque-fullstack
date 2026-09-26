import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';

type TipoMovimento = 1 | 2;
interface Produto { id: number; nome: string; categoriaId: number; categoria: string; preco: number; estoque: number; estoqueMinimo: number; ativo: boolean; estoqueBaixo: boolean; }
interface Categoria { id: number; nome: string; ativo: boolean; }
interface Movimento { id: number; produtoId: number; tipo: TipoMovimento; quantidade: number; dataUtc: string; observacao: string | null; }
interface ProdutoForm { nome: string; categoriaId: number | null; preco: number | null; estoqueMinimo: number | null; }
interface Usuario { id: number; nome: string; email: string; perfilId: number; perfil: string; ativo: boolean; }
interface Perfil { id: number; nome: string; sistema: boolean; ativo: boolean;
  cadastrarProdutos: boolean; gerenciarProdutos: boolean; cadastrarCategorias: boolean;
  gerenciarCategorias: boolean; movimentarEstoque: boolean; }

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  readonly usuarios = signal<Usuario[]>([]);
  readonly perfis = signal<Perfil[]>([]);
  readonly produtos = signal<Produto[]>([]);
  readonly categorias = signal<Categoria[]>([]);
  readonly tela = signal<'visao' | 'produtos' | 'categorias' | 'perfis' | 'usuarios'>('visao');
  readonly movimentos = signal<Movimento[]>([]);
  readonly busca = signal('');
  readonly somenteAtivos = signal(true);
  readonly carregando = signal(false);
  readonly salvando = signal(false);
  readonly erro = signal('');
  readonly sucesso = signal('');
  readonly modal = signal<'produto' | 'categoria' | 'perfil' | 'movimento' | 'historico' | 'usuario' | 'senha' | null>(null);
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
  readonly categoriasAtivas = computed(() => this.categorias().filter(c => c.ativo));

  form: ProdutoForm = this.formVazio();
  categoriaNome = '';
  categoriaSelecionada: Categoria | null = null;
  perfilSelecionado: Perfil | null = null;
  perfilForm = { nome: '', cadastrarProdutos: false, gerenciarProdutos: false,
    cadastrarCategorias: false, gerenciarCategorias: false, movimentarEstoque: false };
  tipoMovimento: TipoMovimento = 1;
  quantidade: number | null = null;
  observacao = '';
  loginEmail = '';
  loginSenha = '';
  novoUsuario = { nome: '', email: '', senha: '', perfilId: 2 };
  senhaAtual = '';
  novaSenha = '';

  ngOnInit(): void { /* Login exigido a cada nova sessão; token fica somente na memória. */ }
  private formVazio(): ProdutoForm { return { nome: '', categoriaId: null, preco: null, estoqueMinimo: 5 }; }
  private mensagemErro(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) return 'Não foi possível conectar à API. Confira se ela está em execução na porta 5029.';
      if (error.status === 401 && this.auth.usuario()) { this.auth.sair(); this.modal.set(null); return 'Sessão expirada. Entre novamente.'; }
      if (error.status === 403) return 'Seu perfil não permite esta operação.';
      return error.error?.erro ?? error.error?.title ?? `Falha na requisição (${error.status}).`;
    }
    return 'Não foi possível concluir a operação.';
  }
  async recarregar(): Promise<void> {
    this.carregando.set(true); this.erro.set('');
    try {
      const [produtos, categorias] = await Promise.all([
        firstValueFrom(this.http.get<Produto[]>('/api/produtos')),
        firstValueFrom(this.http.get<Categoria[]>('/api/categorias'))
      ]);
      this.produtos.set(produtos); this.categorias.set(categorias);
    }
    catch (error) { this.erro.set(this.mensagemErro(error)); }
    finally { this.carregando.set(false); }
  }
  navegar(tela: 'visao' | 'produtos' | 'categorias' | 'perfis' | 'usuarios'): void {
    this.tela.set(tela); this.erro.set(''); this.sucesso.set('');
    if (tela === 'perfis') void this.carregarPerfis();
    if (tela === 'usuarios') void Promise.all([this.carregarUsuarios(), this.carregarPerfis()]);
  }
  private async carregarPerfis(): Promise<void> {
    try { this.perfis.set(await firstValueFrom(this.http.get<Perfil[]>('/api/perfis'))); }
    catch (error) { this.erro.set(this.mensagemErro(error)); }
  }
  novoPerfil(): void {
    this.perfilSelecionado = null;
    this.perfilForm = { nome: '', cadastrarProdutos: false, gerenciarProdutos: false,
      cadastrarCategorias: false, gerenciarCategorias: false, movimentarEstoque: false };
    this.erro.set(''); this.modal.set('perfil');
  }
  editarPerfil(perfil: Perfil): void {
    this.perfilSelecionado = perfil;
    this.perfilForm = { nome: perfil.nome, cadastrarProdutos: perfil.cadastrarProdutos,
      gerenciarProdutos: perfil.gerenciarProdutos, cadastrarCategorias: perfil.cadastrarCategorias,
      gerenciarCategorias: perfil.gerenciarCategorias, movimentarEstoque: perfil.movimentarEstoque };
    this.erro.set(''); this.modal.set('perfil');
  }
  async salvarPerfil(): Promise<void> {
    if (!this.perfilForm.nome.trim()) { this.erro.set('Informe o nome do perfil.'); return; }
    this.salvando.set(true); this.erro.set('');
    try {
      const id = this.perfilSelecionado?.id;
      if (id) await firstValueFrom(this.http.put(`/api/perfis/${id}`, this.perfilForm));
      else await firstValueFrom(this.http.post('/api/perfis', this.perfilForm));
      this.modal.set(null); this.sucesso.set(id ? 'Perfil atualizado.' : 'Perfil cadastrado.');
      await this.carregarPerfis();
    } catch (error) { this.erro.set(this.mensagemErro(error)); }
    finally { this.salvando.set(false); }
  }
  async definirPerfilAtivo(perfil: Perfil): Promise<void> {
    this.erro.set('');
    try { await firstValueFrom(this.http.patch(`/api/perfis/${perfil.id}/ativo`, !perfil.ativo)); await this.carregarPerfis(); }
    catch (error) { this.erro.set(this.mensagemErro(error)); }
  }
  novaCategoria(): void {
    this.categoriaSelecionada = null; this.categoriaNome = '';
    this.erro.set(''); this.modal.set('categoria');
  }
  editarCategoria(categoria: Categoria): void {
    this.categoriaSelecionada = categoria; this.categoriaNome = categoria.nome;
    this.erro.set(''); this.modal.set('categoria');
  }
  async salvarCategoria(): Promise<void> {
    if (!this.categoriaNome.trim()) { this.erro.set('Informe o nome da categoria.'); return; }
    this.salvando.set(true); this.erro.set('');
    try {
      const id = this.categoriaSelecionada?.id;
      if (id) await firstValueFrom(this.http.put(`/api/categorias/${id}`, { nome: this.categoriaNome }));
      else await firstValueFrom(this.http.post('/api/categorias', { nome: this.categoriaNome }));
      this.modal.set(null); this.sucesso.set(id ? 'Categoria atualizada.' : 'Categoria cadastrada.');
      await this.recarregar();
    } catch (error) { this.erro.set(this.mensagemErro(error)); }
    finally { this.salvando.set(false); }
  }
  async definirCategoriaAtiva(categoria: Categoria): Promise<void> {
    this.erro.set('');
    try {
      await firstValueFrom(this.http.patch(`/api/categorias/${categoria.id}/ativo`, !categoria.ativo));
      await this.recarregar();
    } catch (error) { this.erro.set(this.mensagemErro(error)); }
  }
  async entrar(): Promise<void> {
    this.salvando.set(true); this.erro.set('');
    try {
      await this.auth.entrar(this.loginEmail.trim(), this.loginSenha);
      this.loginSenha = '';
      await this.recarregar();
    } catch (error) { this.erro.set(this.mensagemErro(error)); }
    finally { this.salvando.set(false); }
  }
  sair(): void { this.auth.sair(); this.produtos.set([]); this.categorias.set([]); this.tela.set('visao'); this.modal.set(null); this.erro.set(''); this.sucesso.set(''); }
  novoUsuarioForm(): void {
    this.erro.set(''); this.novoUsuario = { nome: '', email: '', senha: '', perfilId: 2 };
    this.modal.set('usuario');
  }
  private async carregarUsuarios(): Promise<void> {
    try { this.usuarios.set(await firstValueFrom(this.http.get<Usuario[]>('/api/usuarios'))); }
    catch (error) { this.erro.set(this.mensagemErro(error)); }
  }
  async criarUsuario(): Promise<void> {
    if (this.novoUsuario.senha.length < 12 || this.novoUsuario.senha.length > 128) {
      this.erro.set('A senha deve ter entre 12 e 128 caracteres.'); return;
    }
    this.salvando.set(true); this.erro.set('');
    try {
      await firstValueFrom(this.http.post('/api/usuarios', this.novoUsuario));
      this.novoUsuario = { nome: '', email: '', senha: '', perfilId: 2 };
      this.modal.set(null); await this.carregarUsuarios(); this.sucesso.set('Usuário cadastrado.');
    } catch (error) { this.erro.set(this.mensagemErro(error)); }
    finally { this.salvando.set(false); }
  }
  async definirAtivo(usuario: Usuario): Promise<void> {
    this.erro.set('');
    try {
      await firstValueFrom(this.http.patch(`/api/usuarios/${usuario.id}/ativo`, !usuario.ativo));
      await this.carregarUsuarios();
    } catch (error) { this.erro.set(this.mensagemErro(error)); }
  }
  async alterarPerfilUsuario(usuario: Usuario, perfilId: number): Promise<void> {
    this.erro.set('');
    try {
      await firstValueFrom(this.http.put(`/api/usuarios/${usuario.id}/perfil`, perfilId));
      await this.carregarUsuarios();
    } catch (error) { this.erro.set(this.mensagemErro(error)); }
  }
  abrirSenha(): void { this.senhaAtual = ''; this.novaSenha = ''; this.erro.set(''); this.modal.set('senha'); }
  async alterarSenha(): Promise<void> {
    this.salvando.set(true); this.erro.set('');
    try { await this.auth.alterarSenha(this.senhaAtual, this.novaSenha); this.modal.set(null); this.produtos.set([]); this.sucesso.set('Senha alterada. Entre novamente.'); }
    catch (error) { this.erro.set(this.mensagemErro(error)); }
    finally { this.salvando.set(false); }
  }
  novoProduto(): void {
    if (!this.categoriasAtivas().length) { this.tela.set('categorias'); this.erro.set('Cadastre uma categoria antes de adicionar produtos.'); return; }
    this.selecionado.set(null); this.form = this.formVazio(); this.erro.set(''); this.modal.set('produto');
  }
  editar(produto: Produto): void {
    this.selecionado.set(produto);
    this.form = { nome: produto.nome, categoriaId: produto.categoriaId, preco: produto.preco, estoqueMinimo: produto.estoqueMinimo };
    this.erro.set(''); this.modal.set('produto');
  }
  async salvarProduto(): Promise<void> {
    if (!this.form.nome.trim() || !this.form.categoriaId || this.form.preco === null || this.form.preco <= 0 ||
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
