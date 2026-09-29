import { Component, input, output } from '@angular/core';
import { Perfil } from '../../shared/models/stock.models';

@Component({ selector: 'app-profiles', templateUrl: './profiles.component.html' })
export class ProfilesComponent {
  readonly perfis = input.required<Perfil[]>();
  readonly editar = output<Perfil>();
  readonly alternarAtivo = output<Perfil>();
}
