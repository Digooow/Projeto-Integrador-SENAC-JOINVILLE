/**
 * CAMADA 3 (SHARED UI) — ProductCardComponent
 * Segue o guia Modern Web Guidance "Build a Card" (<article class="card">):
 * - container-type: inline-size e subgrid de 4 linhas
 * - 1º filho direto: <img> com aspect-ratio: 4/3 e object-fit: cover (sem div .media ao redor)
 * - 2º filho direto: <header> com badges + <h3>
 * - 3º filho direto: <p> com descrição
 * - 4º filho direto: <footer> com preço tabular e botão/stepper nativo
 * - Prioridade LCP (optimize-image-priority): fetchpriority="high" no primeiro card, loading="lazy" nos demais.
 */

import { resolveProductImage } from '../core/product-image.resolver.js';
import { Icons } from './icons.js';

const brlFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL'
});

export function formatCurrency(value) {
  return brlFormatter.format(Number(value || 0));
}

export function buildOfflineSvgPlaceholder(title) {
  const safe = String(title || 'Cantina ALAMO').replace(/[<>&"']/g, '');
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600" viewBox="0 0 800 600">
    <defs>
      <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0%" stop-color="#FFF7ED"/>
        <stop offset="100%" stop-color="#FFEDD5"/>
      </linearGradient>
    </defs>
    <rect width="800" height="600" fill="url(#g)"/>
    <circle cx="400" cy="260" r="110" fill="#FED7AA"/>
    <text x="400" y="450" text-anchor="middle" font-family="sans-serif" font-weight="700" font-size="30" fill="#9A3412">${safe}</text>
  </svg>`;
  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

export function createProductCardElement(product, quantityInCart, onDeltaChange, isPriorityLcp = false) {
  const article = document.createElement('article');
  article.className = `card${product.emPromocao ? ' is-promo' : ''}`;

  // 1. Media: <img> direto como primeiro filho
  const img = document.createElement('img');
  img.src = resolveProductImage(product);
  img.alt = product.nome;
  img.width = 400;
  img.height = 300;
  if (isPriorityLcp) {
    img.setAttribute('fetchpriority', 'high');
  } else {
    img.setAttribute('loading', 'lazy');
  }
  img.addEventListener('error', () => {
    img.src = buildOfflineSvgPlaceholder(product.nome);
  }, { once: true });

  // 2. Header: badges + <h3>
  const header = document.createElement('header');
  const badgesRow = document.createElement('div');
  badgesRow.className = 'card-badges';

  const catBadge = document.createElement('span');
  catBadge.className = 'badge-category';
  catBadge.textContent = product.categoriaNome || 'Cardápio';
  badgesRow.appendChild(catBadge);

  if (product.emPromocao) {
    const promoBadge = document.createElement('span');
    promoBadge.className = 'badge-promo';
    promoBadge.textContent = `-${Math.round(product.percentualDescontoEfetivo)}% Queima`;
    badgesRow.appendChild(promoBadge);
  }

  if (product.quantidadeDisponivel <= 4) {
    const stockBadge = document.createElement('span');
    stockBadge.className = 'badge-stock-low';
    stockBadge.textContent = `Restam ${product.quantidadeDisponivel}`;
    badgesRow.appendChild(stockBadge);
  }

  const h3 = document.createElement('h3');
  h3.textContent = product.nome;

  header.append(badgesRow, h3);

  // 3. Content: <p> direto
  const p = document.createElement('p');
  p.textContent = product.descricao || 'Preparado fresquinho na Cantina ALAMO SENAC Joinville.';

  // 4. Footer: <footer> direto com preço e controles nativos
  const footer = document.createElement('footer');
  const priceGroup = document.createElement('div');
  priceGroup.className = 'price-group';

  if (product.emPromocao) {
    const oldSpan = document.createElement('span');
    oldSpan.className = 'price-old';
    oldSpan.textContent = formatCurrency(product.precoVenda);
    priceGroup.appendChild(oldSpan);
  }

  const currentSpan = document.createElement('span');
  currentSpan.className = `price-current${product.emPromocao ? ' promo' : ''}`;
  currentSpan.textContent = formatCurrency(product.precoFinal);
  priceGroup.appendChild(currentSpan);

  footer.appendChild(priceGroup);

  if (quantityInCart <= 0) {
    const addBtn = document.createElement('button');
    addBtn.type = 'button';
    addBtn.className = 'btn-add-item';
    addBtn.setAttribute('aria-label', `Adicionar ${product.nome} à sacola`);
    addBtn.innerHTML = `${Icons.plus} <span>Adicionar</span>`;
    addBtn.addEventListener('click', () => onDeltaChange(product, 1));
    footer.appendChild(addBtn);
  } else {
    const stepper = document.createElement('div');
    stepper.className = 'qty-stepper';
    stepper.setAttribute('role', 'group');
    stepper.setAttribute('aria-label', `Quantidade de ${product.nome}`);

    const minusBtn = document.createElement('button');
    minusBtn.type = 'button';
    minusBtn.textContent = '−';
    minusBtn.setAttribute('aria-label', `Diminuir quantidade de ${product.nome}`);
    minusBtn.addEventListener('click', () => onDeltaChange(product, -1));

    const qtyText = document.createElement('span');
    qtyText.textContent = String(quantityInCart);

    const plusBtn = document.createElement('button');
    plusBtn.type = 'button';
    plusBtn.textContent = '+';
    plusBtn.disabled = quantityInCart >= product.quantidadeDisponivel;
    plusBtn.setAttribute('aria-label', `Aumentar quantidade de ${product.nome}`);
    plusBtn.addEventListener('click', () => onDeltaChange(product, 1));

    stepper.append(minusBtn, qtyText, plusBtn);
    footer.appendChild(stepper);
  }

  article.append(img, header, p, footer);
  return article;
}

