import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProdutoDto } from '../../core/models/cantina.models';
import { ProductImageResolver } from '../../core/utils/product-image.resolver';

/**
 * CAMADA 3 (SHARED UI) — ProductCardComponent
 * Segue o padrão <article class="card"> com container-type: inline-size e subgrid de 4 linhas:
 * 1) <img> (filho direto, proporção 4:3, object-fit: cover)
 * 2) <header> (badges + título <h3>)
 * 3) <p> (descrição enxuta)
 * 4) <footer> (preço tabular + controle nativo <button> ou stepper)
 */
@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <article class="card" [class.is-promo]="product.emPromocao">
      <img
        [src]="imageUrl"
        [alt]="product.nome"
        [attr.fetchpriority]="isPriorityLcp ? 'high' : null"
        [attr.loading]="isPriorityLcp ? null : 'lazy'"
        width="400"
        height="300"
      />
      <header>
        <div class="card-badges">
          <span class="badge-category">{{ product.categoriaNome }}</span>
          @if (product.emPromocao) {
            <span class="badge-promo">
              -{{ product.percentualDescontoEfetivo | number:'1.0-0' }}% Queima
            </span>
          }
          @if (product.quantidadeDisponivel <= 4) {
            <span class="badge-stock-low">Restam {{ product.quantidadeDisponivel }}</span>
          }
        </div>
        <h3>{{ product.nome }}</h3>
      </header>
      <p>{{ product.descricao || 'Preparado fresquinho na Cantina ALAMO SENAC.' }}</p>
      <footer>
        <div class="price-group">
          @if (product.emPromocao) {
            <span class="price-old">{{ product.precoVenda | currency:'BRL' }}</span>
          }
          <span class="price-current" [class.promo]="product.emPromocao">
            {{ product.precoFinal | currency:'BRL' }}
          </span>
        </div>

        @if (quantityInCart === 0) {
          <button
            type="button"
            class="btn-add-item"
            (click)="changeQuantity.emit(1)"
            [attr.aria-label]="'Adicionar ' + product.nome"
          >
            Adicionar
          </button>
        } @else {
          <div class="qty-stepper" role="group" [attr.aria-label]="'Quantidade de ' + product.nome">
            <button type="button" (click)="changeQuantity.emit(-1)" aria-label="Diminuir">−</button>
            <span>{{ quantityInCart }}</span>
            <button
              type="button"
              (click)="changeQuantity.emit(1)"
              [disabled]="quantityInCart >= product.quantidadeDisponivel"
              aria-label="Aumentar"
            >+</button>
          </div>
        }
      </footer>
    </article>
  `
})
export class ProductCardComponent {
  @Input({ required: true }) public product!: ProdutoDto;
  @Input() public quantityInCart = 0;
  @Input() public isPriorityLcp = false;
  @Output() public readonly changeQuantity = new EventEmitter<number>();

  public get imageUrl(): string {
    return ProductImageResolver.resolve(this.product);
  }
}

