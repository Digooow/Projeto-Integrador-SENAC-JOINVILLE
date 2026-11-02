import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CardapioPublicoDto, CriarPedidoWebRequest, PedidoDto } from '../models/cantina.models';

/**
 * CAMADA 1 (CORE) — Gateway HTTP Tipado para a API .NET 10 (Cantina.Api)
 */
@Injectable({ providedIn: 'root' })
export class CantinaApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1';

  public getCardapio(): Promise<CardapioPublicoDto> {
    return firstValueFrom(this.http.get<CardapioPublicoDto>(`${this.baseUrl}/cardapio`));
  }

  public criarPedido(payload: CriarPedidoWebRequest): Promise<PedidoDto> {
    return firstValueFrom(this.http.post<PedidoDto>(`${this.baseUrl}/pedidos`, payload));
  }

  public consultarPedidoPorCodigo(codigo: string): Promise<PedidoDto> {
    const clean = encodeURIComponent(codigo.trim().toUpperCase());
    return firstValueFrom(this.http.get<PedidoDto>(`${this.baseUrl}/pedidos/${clean}`));
  }
}

