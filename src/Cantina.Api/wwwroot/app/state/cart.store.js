/**
 * CAMADA 2 (STATE) — CartStore Reativa
 */

import { signal, computed } from './signals.js';

export function createCartStore() {
  const itemsMap = signal(new Map());

  const items = computed(() => Array.from(itemsMap().values()));

  const totalItems = computed(() =>
    items().reduce((sum, entry) => sum + entry.quantity, 0)
  );

  const subtotalOriginal = computed(() =>
    items().reduce((sum, entry) => sum + entry.product.precoVenda * entry.quantity, 0)
  );

  const totalFinal = computed(() =>
    items().reduce((sum, entry) => sum + entry.product.precoFinal * entry.quantity, 0)
  );

  const totalSavings = computed(() =>
    Math.max(0, subtotalOriginal() - totalFinal())
  );

  function getQuantity(productId) {
    return itemsMap().get(productId)?.quantity ?? 0;
  }

  function setDelta(product, delta) {
    const next = new Map(itemsMap());
    const current = next.get(product.id)?.quantity ?? 0;
    const updated = Math.min(product.quantidadeDisponivel, Math.max(0, current + delta));

    if (updated <= 0) {
      next.delete(product.id);
    } else {
      next.set(product.id, { product, quantity: updated });
    }
    itemsMap.set(next);
  }

  function syncWithCatalog(products) {
    const byId = new Map((products || []).map((p) => [p.id, p]));
    const next = new Map();
    for (const [id, entry] of itemsMap().entries()) {
      const fresh = byId.get(id);
      if (fresh && fresh.ativo && fresh.quantidadeDisponivel > 0) {
        next.set(id, {
          product: fresh,
          quantity: Math.min(entry.quantity, fresh.quantidadeDisponivel)
        });
      }
    }
    itemsMap.set(next);
  }

  function clear() {
    itemsMap.set(new Map());
  }

  return {
    items,
    totalItems,
    subtotalOriginal,
    totalFinal,
    totalSavings,
    getQuantity,
    setDelta,
    syncWithCatalog,
    clear
  };
}

