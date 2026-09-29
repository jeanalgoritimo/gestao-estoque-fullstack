import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({ selector: 'app-login', imports: [FormsModule], templateUrl: './login.component.html' })
export class LoginComponent {
  readonly erro = input.required<string>();
  readonly sucesso = input.required<string>();
  readonly salvando = input.required<boolean>();
  readonly entrar = output<{ email: string; senha: string }>();
  email = '';
  senha = '';
}
