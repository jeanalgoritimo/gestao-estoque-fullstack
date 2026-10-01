import { agruparSaldos } from './shared/models/stock-totals';
import { UnidadeMedida } from './shared/models/unit.models';
import { UnitsComponent } from './features/units/units.component';
import { Almoxarifado, PosicaoEstoque } from './shared/models/location.models';
import { LocationsComponent } from './features/locations/locations.component';
import { CostCentersComponent } from './features/cost-centers/cost-centers.component';
import { ConsumptionComponent } from './features/cost-centers/consumption.component';
import { CentroCusto } from './shared/models/cost-center.models';
import { RequisitionsComponent } from './features/requisitions/requisitions.component';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from './core/auth/auth.service';
import { LoginComponent } from './features/auth/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { UsersComponent } from './features/access/users.component';
import { ProfilesComponent } from './features/access/profiles.component';
import { CategoriesComponent } from './features/categories/categories.component';
import { ProductsComponent } from './features/products/products.component';
import { InventoryComponent } from './features/inventory/inventory.component';
import { SuppliersComponent } from './features/suppliers/suppliers.component';
import { ReportsComponent } from './features/reports/reports.component';
import { ReplenishmentComponent } from './features/replenishment/replenishment.component';
import { PurchaseOrdersComponent } from './features/replenishment/purchase-orders.component';

import { EstoqueApiService } from './core/api/estoque-api.service';

import {
  Categoria,
  Fornecedor,
  Movimento,
  Perfil,
  Produto,
  ProdutoForm,
  TipoMovimento,
  Usuario,
} from './shared/models/stock.models';

@Component({
  selector: 'app-root',
  imports: [
    CommonModule,
    FormsModule,
    LoginComponent,
    DashboardComponent,
    UsersComponent,
    ProfilesComponent,
    CategoriesComponent,
    ProductsComponent,
    InventoryComponent,
    SuppliersComponent,
    ReportsComponent,
    ReplenishmentComponent,
    PurchaseOrdersComponent,
    RequisitionsComponent,
    CostCentersComponent,
    ConsumptionComponent,
    LocationsComponent,
    UnitsComponent,
  ],
  templateUrl: './app.html',
})
export class App {
  private readonly api = inject(EstoqueApiService);
  readonly auth = inject(AuthService);
  readonly usuarios = signal<Usuario[]>([]);
  readonly perfis = signal<Perfil[]>([]);
  readonly produtos = signal<Produto[]>([]);
  readonly categorias = signal<Categoria[]>([]);
  readonly unidadesMedida = signal<UnidadeMedida[]>([]);
  unidadePorId(id: number): string { return this.unidadesMedida().find(u => u.id === id)?.sigla ?? 'UN'; }
  readonly almoxarifados = signal<Almoxarifado[]>([]);
  readonly posicoes = signal<PosicaoEstoque[]>([]);
  readonly filtroPosicao = signal('todos');
  readonly centrosCusto = signal<CentroCusto[]>([]);
  readonly fornecedores = signal<Fornecedor[]>([]);
  readonly tela = signal<
    'visao' | 'produtos' | 'categorias' | 'inventarios' | 'fornecedores' | 'reposicao' | 'pedidos' | 'requisicoes' | 'centros' | 'consumo' | 'relatorios' | 'perfis' | 'usuarios' | 'localizacoes' | 'unidades'
  >('visao');
  readonly menuRecolhido = signal(false);
  readonly menuMobileAberto = signal(false);
  readonly movimentos = signal<Movimento[]>([]);
  readonly busca = signal('');
  readonly somenteAtivos = signal(true);
  readonly carregando = signal(false);
  readonly salvando = signal(false);
  readonly erro = signal('');
  readonly sucesso = signal('');
  readonly modal = signal<
    'produto' | 'categoria' | 'perfil' | 'movimento' | 'historico' | 'usuario' | 'senha' | null
  >(null);
  readonly selecionado = signal<Produto | null>(null);
  readonly filtrados = computed(() => {
    const termo = this.busca().trim().toLocaleLowerCase('pt-BR');
    return this.produtos().filter(
      (p) =>
        (!this.somenteAtivos() || p.ativo) &&
        (this.filtroPosicao() === 'todos' || (this.filtroPosicao() === 'sem' ? p.posicaoEstoqueId === null : p.posicaoEstoqueId === Number(this.filtroPosicao()))) &&
        (!termo || `${p.nome} ${p.categoria} ${p.id} ${p.localizacao}`.toLocaleLowerCase('pt-BR').includes(termo)),
    );
  });
  readonly ativos = computed(() => this.produtos().filter((p) => p.ativo));
  readonly baixos = computed(() => this.ativos().filter((p) => p.estoqueBaixo).length);
  readonly saldos = computed(() => agruparSaldos(this.ativos()));
  readonly potencialVendas = computed(() =>
    this.ativos().reduce((total, p) => total + p.estoque * p.preco, 0),
  );
  readonly categoriasAtivas = computed(() => this.categorias().filter((c) => c.ativo));

  form: ProdutoForm = this.formVazio();
  categoriaNome = '';
  categoriaSelecionada: Categoria | null = null;
  perfilSelecionado: Perfil | null = null;
  perfilForm = {
    nome: '',
    cadastrarProdutos: false,
    gerenciarProdutos: false,
    cadastrarCategorias: false,
    gerenciarCategorias: false,
    movimentarEstoque: false,
  };
  tipoMovimento: TipoMovimento = 1;
  quantidade: number | null = null;
  observacao = '';
  documentoOrigem = '';
  motivo = '';
  dataEfetiva = '';
  custoUnitario: number | null = null;
  novoUsuario = { nome: '', email: '', senha: '', perfilId: 2 };
  senhaAtual = '';
  novaSenha = '';

  private formVazio(): ProdutoForm {
    return { unidadeMedidaId: 1, posicaoEstoqueId: null, nome: '', categoriaId: null, fornecedorId: null, preco: null, estoqueMinimo: 5 };
  }
  private mensagemErro(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0)
        return 'Não foi possível conectar à API. Confira se ela está em execução na porta 5029.';
      if (error.status === 401 && this.auth.usuario()) {
        this.auth.sair();
        this.modal.set(null);
        return 'Sessão expirada. Entre novamente.';
      }
      if (error.status === 403) return 'Seu perfil não permite esta operação.';
      return error.error?.erro ?? error.error?.title ?? `Falha na requisição (${error.status}).`;
    }
    return 'Não foi possível concluir a operação.';
  }
  async recarregar(): Promise<void> {
    this.carregando.set(true);
    this.erro.set('');
    try {
      const [produtos, categorias, fornecedores, centros, almoxarifados, posicoes, unidades] = await Promise.all([
        this.api.produtos(),
        this.api.categorias(),
        this.api.fornecedores(),
        this.api.centrosCusto(),
        this.api.almoxarifados(),
        this.api.posicoes(),
        this.api.unidadesMedida(),
      ]);
      this.produtos.set(produtos);
      this.categorias.set(categorias);
      this.fornecedores.set(fornecedores);
      this.centrosCusto.set(centros);
      this.almoxarifados.set(almoxarifados); this.posicoes.set(posicoes); this.unidadesMedida.set(unidades);
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    } finally {
      this.carregando.set(false);
    }
  }
  navegar(tela: 'visao' | 'produtos' | 'categorias' | 'inventarios' | 'fornecedores' | 'reposicao' | 'pedidos' | 'requisicoes' | 'centros' | 'consumo' | 'relatorios' | 'perfis' | 'usuarios' | 'localizacoes' | 'unidades'): void {
    this.tela.set(tela);
    this.erro.set('');
    this.sucesso.set('');
    this.menuMobileAberto.set(false);
    if (tela === 'perfis') void this.carregarPerfis();
    if (tela === 'usuarios') void Promise.all([this.carregarUsuarios(), this.carregarPerfis()]);
  }
  private async carregarPerfis(): Promise<void> {
    try {
      this.perfis.set(await this.api.perfis());
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    }
  }
  novoPerfil(): void {
    this.perfilSelecionado = null;
    this.perfilForm = {
      nome: '',
      cadastrarProdutos: false,
      gerenciarProdutos: false,
      cadastrarCategorias: false,
      gerenciarCategorias: false,
      movimentarEstoque: false,
    };
    this.erro.set('');
    this.modal.set('perfil');
  }
  editarPerfil(perfil: Perfil): void {
    this.perfilSelecionado = perfil;
    this.perfilForm = {
      nome: perfil.nome,
      cadastrarProdutos: perfil.cadastrarProdutos,
      gerenciarProdutos: perfil.gerenciarProdutos,
      cadastrarCategorias: perfil.cadastrarCategorias,
      gerenciarCategorias: perfil.gerenciarCategorias,
      movimentarEstoque: perfil.movimentarEstoque,
    };
    this.erro.set('');
    this.modal.set('perfil');
  }
  async salvarPerfil(): Promise<void> {
    if (!this.perfilForm.nome.trim()) {
      this.erro.set('Informe o nome do perfil.');
      return;
    }
    this.salvando.set(true);
    this.erro.set('');
    try {
      const id = this.perfilSelecionado?.id;
      if (id) await this.api.atualizarPerfil(id, this.perfilForm);
      else await this.api.criarPerfil(this.perfilForm);
      this.modal.set(null);
      this.sucesso.set(id ? 'Perfil atualizado.' : 'Perfil cadastrado.');
      await this.carregarPerfis();
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    } finally {
      this.salvando.set(false);
    }
  }
  async definirPerfilAtivo(perfil: Perfil): Promise<void> {
    this.erro.set('');
    try {
      await this.api.definirPerfilAtivo(perfil.id, !perfil.ativo);
      await this.carregarPerfis();
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    }
  }
  novaCategoria(): void {
    this.categoriaSelecionada = null;
    this.categoriaNome = '';
    this.erro.set('');
    this.modal.set('categoria');
  }
  editarCategoria(categoria: Categoria): void {
    this.categoriaSelecionada = categoria;
    this.categoriaNome = categoria.nome;
    this.erro.set('');
    this.modal.set('categoria');
  }
  async salvarCategoria(): Promise<void> {
    if (!this.categoriaNome.trim()) {
      this.erro.set('Informe o nome da categoria.');
      return;
    }
    this.salvando.set(true);
    this.erro.set('');
    try {
      const id = this.categoriaSelecionada?.id;
      if (id) await this.api.atualizarCategoria(id, this.categoriaNome);
      else await this.api.criarCategoria(this.categoriaNome);
      this.modal.set(null);
      this.sucesso.set(id ? 'Categoria atualizada.' : 'Categoria cadastrada.');
      await this.recarregar();
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    } finally {
      this.salvando.set(false);
    }
  }
  async definirCategoriaAtiva(categoria: Categoria): Promise<void> {
    this.erro.set('');
    try {
      await this.api.definirCategoriaAtiva(categoria.id, !categoria.ativo);
      await this.recarregar();
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    }
  }
  async entrar(email: string, senha: string): Promise<void> {
    this.salvando.set(true);
    this.erro.set('');
    try {
      await this.auth.entrar(email, senha);
      await this.recarregar();
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    } finally {
      this.salvando.set(false);
    }
  }
  sair(): void {
    this.auth.sair();
    this.produtos.set([]);
    this.categorias.set([]);
    this.tela.set('visao');
    this.menuMobileAberto.set(false);
    this.modal.set(null);
    this.erro.set('');
    this.sucesso.set('');
  }
  novoUsuarioForm(): void {
    this.erro.set('');
    this.novoUsuario = { nome: '', email: '', senha: '', perfilId: 2 };
    this.modal.set('usuario');
  }
  private async carregarUsuarios(): Promise<void> {
    try {
      this.usuarios.set(await this.api.usuarios());
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    }
  }
  async criarUsuario(): Promise<void> {
    if (this.novoUsuario.senha.length < 12 || this.novoUsuario.senha.length > 128) {
      this.erro.set('A senha deve ter entre 12 e 128 caracteres.');
      return;
    }
    this.salvando.set(true);
    this.erro.set('');
    try {
      await this.api.criarUsuario(this.novoUsuario);
      this.novoUsuario = { nome: '', email: '', senha: '', perfilId: 2 };
      this.modal.set(null);
      await this.carregarUsuarios();
      this.sucesso.set('Usuário cadastrado.');
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    } finally {
      this.salvando.set(false);
    }
  }
  async definirAtivo(usuario: Usuario): Promise<void> {
    this.erro.set('');
    try {
      await this.api.definirUsuarioAtivo(usuario.id, !usuario.ativo);
      await this.carregarUsuarios();
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    }
  }
  async alterarPerfilUsuario(usuario: Usuario, perfilId: number): Promise<void> {
    this.erro.set('');
    try {
      await this.api.alterarPerfilUsuario(usuario.id, perfilId);
      await this.carregarUsuarios();
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    }
  }
  abrirSenha(): void {
    this.senhaAtual = '';
    this.novaSenha = '';
    this.erro.set('');
    this.modal.set('senha');
  }
  async alterarSenha(): Promise<void> {
    this.salvando.set(true);
    this.erro.set('');
    try {
      await this.auth.alterarSenha(this.senhaAtual, this.novaSenha);
      this.modal.set(null);
      this.produtos.set([]);
      this.sucesso.set('Senha alterada. Entre novamente.');
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    } finally {
      this.salvando.set(false);
    }
  }
  novoProduto(): void {
    if (!this.categoriasAtivas().length) {
      this.tela.set('categorias');
      this.erro.set('Cadastre uma categoria antes de adicionar produtos.');
      return;
    }
    this.selecionado.set(null);
    this.form = this.formVazio();
    this.erro.set('');
    this.modal.set('produto');
  }
  editar(produto: Produto): void {
    this.selecionado.set(produto);
    this.form = {
      unidadeMedidaId: produto.unidadeMedidaId,
      posicaoEstoqueId: produto.posicaoEstoqueId,
      nome: produto.nome,
      categoriaId: produto.categoriaId,
      fornecedorId: produto.fornecedorId,
      preco: produto.preco,
      estoqueMinimo: produto.estoqueMinimo,
    };
    this.erro.set('');
    this.modal.set('produto');
  }
  async salvarProduto(): Promise<void> {
    if (
      !this.form.nome.trim() ||
      !this.form.categoriaId ||
      this.form.preco === null ||
      this.form.preco <= 0 ||
      this.form.estoqueMinimo === null ||
      !Number.isInteger(this.form.estoqueMinimo) ||
      this.form.estoqueMinimo < 0
    ) {
      this.erro.set('Informe nome, categoria, preço maior que zero e estoque mínimo válido.');
      return;
    }
    this.salvando.set(true);
    this.erro.set('');
    try {
      const id = this.selecionado()?.id;
      if (id) await this.api.atualizarProduto(id, this.form);
      else await this.api.criarProduto(this.form);
      this.modal.set(null);
      this.sucesso.set(
        id
          ? 'Produto atualizado.'
          : 'Produto cadastrado. Registre uma entrada para definir o saldo.',
      );
      await this.recarregar();
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    } finally {
      this.salvando.set(false);
    }
  }
  abrirMovimento(produto: Produto, tipo: TipoMovimento): void {
    this.selecionado.set(produto);
    this.tipoMovimento = tipo;
    this.quantidade = null;
    this.observacao = '';
    this.documentoOrigem = '';
    this.motivo = '';
    this.custoUnitario = null;
    this.dataEfetiva = '';
    this.erro.set('');
    this.modal.set('movimento');
  }
  async salvarMovimento(): Promise<void> {
    const produto = this.selecionado();
    if (
      !produto ||
      this.quantidade === null ||
      !Number.isInteger(this.quantidade) ||
      this.quantidade <= 0 ||
      !this.motivo.trim() ||
      (this.custoUnitario !== null &&
        (!Number.isFinite(this.custoUnitario) || this.custoUnitario < 0))
    ) {
      this.erro.set('Informe quantidade inteira positiva, motivo e custo unitário válido.');
      return;
    }
    this.salvando.set(true);
    this.erro.set('');
    try {
      await this.api.criarMovimento(produto.id, {
        tipo: this.tipoMovimento,
        quantidade: this.quantidade,
        observacao: this.observacao || null,
        documentoOrigem: this.documentoOrigem,
        motivo: this.motivo,
        dataEfetivaUtc: this.dataEfetiva ? new Date(this.dataEfetiva).toISOString() : null,
        custoUnitario: this.custoUnitario,
      });
      this.modal.set(null);
      this.sucesso.set('Movimentação registrada.');
      await this.recarregar();
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    } finally {
      this.salvando.set(false);
    }
  }
  async abrirHistorico(produto: Produto): Promise<void> {
    this.selecionado.set(produto);
    this.movimentos.set([]);
    this.erro.set('');
    this.modal.set('historico');
    try {
      this.movimentos.set(await this.api.movimentos(produto.id));
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    }
  }
  async desativar(produto: Produto): Promise<void> {
    if (!confirm(`Desativar ${produto.nome}? O histórico será mantido.`)) return;
    this.erro.set('');
    try {
      await this.api.desativarProduto(produto.id);
      this.sucesso.set('Produto desativado.');
      await this.recarregar();
    } catch (error) {
      this.erro.set(this.mensagemErro(error));
    }
  }
  fechar(): void {
    if (!this.salvando()) {
      this.modal.set(null);
      this.erro.set('');
    }
  }
  moeda(valor: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor);
  }
}
