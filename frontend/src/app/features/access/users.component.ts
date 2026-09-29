import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Perfil, Usuario } from '../../shared/models/stock.models';

@Component({ selector: 'app-users', imports: [FormsModule], templateUrl: './users.component.html' })
export class UsersComponent {
  readonly usuarios = input.required<Usuario[]>();
  readonly perfis = input.required<Perfil[]>();
  readonly usuarioAtualId = input.required<number>();
  readonly alterarPerfil = output<{ usuario: Usuario; perfilId: number }>();
  readonly alternarAtivo = output<Usuario>();
}
