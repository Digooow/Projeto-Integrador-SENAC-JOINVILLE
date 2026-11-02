import { Injectable, computed, signal } from '@angular/core';
import { CartItem, ProdutoDto } from '../core/models/cantina.models';

/**
 * CAMADA 2 (STATE) — Store Reativa da Sacola de Compras (Angular Signals)
 */
@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly itemsMap = signal<ReadonlyMap<number, CartItem>>(new Map());

  public readonly items = computed(() => Array.from(this.itemsMap().values()));

  public readonly totalItems = computed(() =>
    this.items().reduce((acc, item) => acc + item.quantity, 0)
  );

  public readonly subtotalOriginal = computed(() =>
    this.items().reduce((acc, item) => acc + item.product.precoVenda * item.quantity, 0)
  );

  public readonly totalFinal = computed(() =>
    this.items().reduce((acc, item) => acc + item.product.precoFinal * item.quantity, 0)
  );

  public readonly totalSavings = computed(() =>
    Math.max(0, this.subtotalOriginal() - this.totalFinal())
  );

  public getQuantity(productId: number): number {
    return this.itemsMap().get(productId)?.quantity ?? 0;
  }

  public setDelta(product: ProdutoDto, delta: number): void {
    const next = new Map(this.itemsMap());
    const current = next.get(product.id)?.quantity ?? 0;
    const updated = Math.min(product.quantidadeDisponivel, Math.max(0, current + delta));

    if (updated <= 0) {
      next.delete(product.id);
    } else {
      next.set(product.id, { product, quantity: updated });
    }
    this.itemsMap.set(next);
  }

  public syncWithCatalog(products: readonly ProdutoDto[]): void {
    const byId = new Map(products.map((p) => [p.id, p]));
    const next = new Map<number, CartItem>();

    for (const [id, entry] of this.itemsMap().entries()) {
      const fresh = byId.get(id);
      if (fresh && fresh.ativo && fresh.quantidadeDisponivel > 0) {
        next.set(id, {
          product: fresh,
          quantity: Math.min(entry.quantity, fresh.quantidadeDisponivel)
        });
      }
    }
    this.itemsMap.set(next);
  }

  public clear(): void {
    this.itemsMap.set(new Map());
  }
}

