import { Injectable, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { CantinaApiService } from '../core/api/cantina-api.service';
import { PedidoDto } from '../core/models/cantina.models';

/**
 * CAMADA 2 (STATE) — Store de Acompanhamento de Pedido (#AL-XXXX)
 */
@Injectable({ providedIn: 'root' })
export class OrderTrackerStore {
  private readonly api = inject(CantinaApiService);

  public readonly activeOrder = signal<PedidoDto | null>(null);
  public readonly isSearching = signal<boolean>(false);
  public readonly errorMessage = signal<string>('');

  public async lookupOrder(code: string): Promise<PedidoDto | null> {
    this.isSearching.set(true);
    this.errorMessage.set('');
    try {
      const order = await this.api.consultarPedidoPorCodigo(code);
      this.activeOrder.set(order);
      return order;
    } catch (error: unknown) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        this.errorMessage.set('Pedido não localizado. Confira o código #AL-XXXX informado.');
      } else {
        this.errorMessage.set('Falha na comunicação com o servidor. Verifique sua conexão e tente novamente.');
      }
      return null;
    } finally {
      this.isSearching.set(false);
    }
  }
}

