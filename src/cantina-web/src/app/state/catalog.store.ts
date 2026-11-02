import { Injectable, computed, inject, signal } from '@angular/core';
import { CantinaApiService } from '../core/api/cantina-api.service';
import { CategoriaDto, GrupoWhatsAppDto, ProdutoDto } from '../core/models/cantina.models';

/**
 * CAMADA 2 (STATE) — Store Reativa do Catálogo (Angular Signals)
 */
@Injectable({ providedIn: 'root' })
export class CatalogStore {
  private readonly api = inject(CantinaApiService);

  public readonly isLoading = signal<boolean>(true);
  public readonly categories = signal<readonly CategoriaDto[]>([]);
  public readonly products = signal<readonly ProdutoDto[]>([]);
  public readonly offers = signal<readonly ProdutoDto[]>([]);
  public readonly whatsappGroups = signal<readonly GrupoWhatsAppDto[]>([]);
  public readonly queimaAtivaAgora = signal<boolean>(false);
  public readonly horarioProgramadoQueima = signal<string>('20:45');

  public readonly selectedCategoryId = signal<number | 'all'>('all');
  public readonly searchQuery = signal<string>('');
  public readonly onlyOffers = signal<boolean>(false);

  public readonly filteredProducts = computed(() => {
    const category = this.selectedCategoryId();
    const query = this.searchQuery().trim().toLowerCase();
    const offersOnly = this.onlyOffers();

    return this.products().filter((p) => {
      if (category !== 'all' && p.categoriaId !== category) return false;
      if (offersOnly && !p.emPromocao) return false;
      if (query) {
        const text = `${p.nome} ${p.descricao} ${p.categoriaNome}`.toLowerCase();
        if (!text.includes(query)) return false;
      }
      return true;
    });
  });

  public async loadCatalog(silent = false): Promise<void> {
    if (!silent) this.isLoading.set(true);
    try {
      const data = await this.api.getCardapio();
      this.categories.set(data.categorias ?? []);
      this.products.set(data.produtos ?? []);
      this.offers.set(data.ofertasDoDia ?? []);
      this.whatsappGroups.set(data.gruposWhatsApp ?? []);
      this.queimaAtivaAgora.set(Boolean(data.queimaAtivaAgora));
      this.horarioProgramadoQueima.set(data.horarioProgramadoQueima || '20:45');
    } finally {
      this.isLoading.set(false);
    }
  }
}

