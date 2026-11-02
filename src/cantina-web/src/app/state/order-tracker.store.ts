import { Injectable, inject, signal } from '@angular/core';
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
    } catch {
      this.errorMessage.set('Pedido não localizado. Confira o código #AL-XXXX informado.');
      return null;
    } finally {
      this.isSearching.set(false);
    }
  }
}

