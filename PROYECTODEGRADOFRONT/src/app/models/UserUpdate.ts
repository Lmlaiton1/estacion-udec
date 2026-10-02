export interface UserUpdate {
  id: string;
  name: string;
  last_Name: string;
  id_Card: string;
  activo: boolean;
  // Nombre del rol (no el Guid) - así lo espera User_PostDto en el backend
  rol: string;
}
