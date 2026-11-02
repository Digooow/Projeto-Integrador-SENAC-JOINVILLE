/**
 * CAMADA 2 (STATE) — CatalogStore & CartStore Reativas
 */

import { signal, computed } from './signals.js';
import { CantinaApiService } from '../core/cantina-api.service.js';

export function createCatalogStore() {
  const isLoading = signal(true);
  const categories = signal([]);
  const products = signal([]);
  const offers = signal([]);
  const whatsappGroups = signal([]);
  const queimaAtivaAgora = signal(false);
  const horarioProgramadoQueima = signal('20:45');

  const selectedCategoryId = signal('all');
  const searchQuery = signal('');
  const onlyOffers = signal(false);

  const filteredProducts = computed(() => {
    const cat = selectedCategoryId();
    const query = searchQuery().trim().toLowerCase();
    const promoOnly = onlyOffers();

    return products().filter((p) => {
      if (cat !== 'all' && String(p.categoriaId) !== String(cat)) return false;
      if (promoOnly && !p.emPromocao) return false;
      if (query) {
        const text = `${p.nome} ${p.descricao} ${p.categoriaNome}`.toLowerCase();
        if (!text.includes(query)) return false;
      }
      return true;
    });
  });

  let lastCatalogHash = '';

  async function loadCatalog(silent = false) {
    if (!silent) isLoading.set(true);
    try {
      const data = await CantinaApiService.getCardapio();
      const nextHash = JSON.stringify({
        q: data.queimaAtivaAgora,
        p: (data.produtos || []).map((x) => `${x.id}:${x.precoFinal}:${x.quantidadeDisponivel}:${x.emPromocao}`),
        g: (data.gruposWhatsApp || []).length
      });

      if (silent && nextHash === lastCatalogHash) {
        return false;
      }
      lastCatalogHash = nextHash;

      categories.set(data.categorias || []);
      products.set(data.produtos || []);
      offers.set(data.ofertasDoDia || []);
      whatsappGroups.set(data.gruposWhatsApp || []);
      queimaAtivaAgora.set(Boolean(data.queimaAtivaAgora));
      horarioProgramadoQueima.set(data.horarioProgramadoQueima || '20:45');
      return true;
    } finally {
      isLoading.set(false);
    }
  }

  return {
    isLoading,
    categories,
    products,
    offers,
    whatsappGroups,
    queimaAtivaAgora,
    horarioProgramadoQueima,
    selectedCategoryId,
    searchQuery,
    onlyOffers,
    filteredProducts,
    loadCatalog
  };
}

