import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CatalogStore } from './state/catalog.store';
import { CartStore } from './state/cart.store';
import { OrderTrackerStore } from './state/order-tracker.store';
import { ProductCardComponent } from './shared/ui/product-card.component';

/**
 * CAMADA 4 (FEATURES) — Shell Principal da Vitrine Foodtech Cantina ALAMO
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, ProductCardComponent],
  template: `
    <div class="app-shell">
      <main class="container">
        <section class="products-grid">
          @for (prod of catalog.filteredProducts(); track prod.id; let i = $index) {
            <app-product-card
              [product]="prod"
              [quantityInCart]="cart.getQuantity(prod.id)"
              [isPriorityLcp]="i === 0"
              (changeQuantity)="cart.setDelta(prod, $event)"
            />
          }
        </section>
      </main>
    </div>
  `
})
export class AppComponent implements OnInit {
  public readonly catalog = inject(CatalogStore);
  public readonly cart = inject(CartStore);
  public readonly tracker = inject(OrderTrackerStore);

  public async ngOnInit(): Promise<void> {
    await this.catalog.loadCatalog();
  }
}

