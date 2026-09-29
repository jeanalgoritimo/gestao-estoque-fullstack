import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from './core/auth/auth.service';
import { LoginComponent } from './features/auth/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { UsersComponent } from './features/access/users.component';
import { ProfilesComponent } from './features/access/profiles.component';
import { CategoriesComponent } from './feat…3288 tokens truncated…    this.quantidade <= 0 ||
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
