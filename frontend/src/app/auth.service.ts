import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface UsuarioLogado {
  id: number; nome: string; email: string; perfilId: number; perfil: string;
  permissoes: { gerenciarProdutos: boolean; gerenciarCategorias: boolean; movimentarEstoque: boolean };
}
interface LoginResponse { token: string; expiraUtc: string; usuario: UsuarioLogado; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  readonly usuario = signal<UsuarioLogado | null>(null);
  private token: string | null = null;
  private expira = 0;

  get accessToken(): string | null {
    if (this.token && Date.now() >= this.expira) this.sair();
    return this.token;
  }

  async entrar(email: string, senha: string): Promise<void> {
    const result = await firstValueFrom(this.http.post<LoginResponse>('/api/auth/login', { email, senha }));
    this.token = result.token;
    this.expira = new Date(result.expiraUtc).getTime();
    this.usuario.set(result.usuario);
  }

  sair(): void { this.token = null; this.expira = 0; this.usuario.set(null); }

  async alterarSenha(senhaAtual: string, novaSenha: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/alterar-senha', { senhaAtual, novaSenha }));
    this.sair();
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken;
  return next(token && req.url.startsWith('/api/') ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);
};
