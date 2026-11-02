/**
 * CAMADA 4 (FEATURES) — Orquestrador Principal da Vitrine Foodtech Cantina ALAMO
 * Consome as Stores Reativas (CatalogStore, CartStore), renderiza os componentes da Camada 3
 * e aplica os fallbacks obrigatórios do Modern Web Guidance.
 */

import { effect } from './state/signals.js';
import { createCatalogStore } from './state/catalog.store.js';
import { createCartStore } from './state/cart.store.js';
import { CantinaApiService } from './core/cantina-api.service.js';
import { resolveProductImage } from './core/product-image.resolver.js';
import {
  createProductCardElement,
  formatCurrency,
  buildOfflineSvgPlaceholder
} from './ui/product-card.component.js';
import { Icons } from './ui/icons.js';

const catalog = createCatalogStore();
const cart = createCartStore();

// Referências DOM principais
const dom = {
  heroBannerCard: document.getElementById('heroBannerCard'),
  heroStatusBadge: document.getElementById('heroStatusBadge'),
  heroCountdownText: document.getElementById('heroCountdownText'),
  heroTitle: document.getElementById('heroTitle'),
  heroSubtitle: document.getElementById('heroSubtitle'),
  showcaseImg: document.getElementById('showcaseImg'),
  showcaseTitle: document.getElementById('showcaseTitle'),
  showcaseBadge: document.getElementById('showcaseBadge'),
  quickTrackInput: document.getElementById('quickTrackInput'),
  btnQuickTrack: document.getElementById('btnQuickTrack'),

  categoryPills: document.getElementById('categoryPills'),
  searchInput: document.getElementById('searchInput'),
  onlyOffersToggle: document.getElementById('onlyOffersToggle'),

  promoSection: document.getElementById('promoSection'),
  promoGrid: document.getElementById('promoGrid'),
  promoCountPill: document.getElementById('promoCountPill'),

  catalogGrid: document.getElementById('catalogGrid'),
  catalogCountPill: document.getElementById('catalogCountPill'),

  whatsappGroupsGrid: document.getElementById('whatsappGroupsGrid'),

  btnNavCart: document.getElementById('btnNavCart'),
  navCartBadge: document.getElementById('navCartBadge'),
  navCartTotal: document.getElementById('navCartTotal'),

  floatingCartPill: document.getElementById('floatingCartPill'),
  floatingCartItems: document.getElementById('floatingCartItems'),
  floatingCartTotal: document.getElementById('floatingCartTotal'),
  btnOpenDrawerFloating: document.getElementById('btnOpenDrawerFloating'),

  // Slide-Over Drawer da Sacola (<dialog closedby="any">)
  cartDrawerModal: document.getElementById('cartDrawerModal'),
  btnCloseCartDrawer: document.getElementById('btnCloseCartDrawer'),
  drawerCartItems: document.getElementById('drawerCartItems'),
  drawerSubtotal: document.getElementById('drawerSubtotal'),
  drawerSavingsRow: document.getElementById('drawerSavingsRow'),
  drawerSavings: document.getElementById('drawerSavings'),
  drawerGrandTotal: document.getElementById('drawerGrandTotal'),
  checkoutForm: document.getElementById('checkoutForm'),
  btnSubmitOrder: document.getElementById('btnSubmitOrder'),
  checkoutFeedback: document.getElementById('checkoutFeedback'),

  // Modal de Acompanhamento / Ticket Digital (<dialog closedby="any">)
  btnOpenTrackerNav: document.getElementById('btnOpenTrackerNav'),
  trackerModal: document.getElementById('trackerModal'),
  btnCloseTrackerModal: document.getElementById('btnCloseTrackerModal'),
  trackerForm: document.getElementById('trackerForm'),
  trackerCodeInput: document.getElementById('trackerCodeInput'),
  trackerTicketContainer: document.getElementById('trackerTicketContainer')
};

/* ==========================================================================
   1. RENDERIZAÇÃO REATIVA DAS SEÇÕES (SIGNALS -> DOM)
   ========================================================================== */

function renderHeroAndShowcase() {
  const isPromoLive = catalog.queimaAtivaAgora();
  const horario = catalog.horarioProgramadoQueima();
  const offers = catalog.offers();
  const allProducts = catalog.products();

  dom.heroBannerCard.classList.toggle('promo-live', isPromoLive);
  dom.heroStatusBadge.classList.toggle('is-live', isPromoLive);

  if (isPromoLive || offers.length > 0) {
    dom.heroStatusBadge.innerHTML = `${Icons.flame} <span>QUEIMA DE ESTOQUE ATIVA AGORA</span>`;
    dom.heroTitle.innerHTML = `Salgados e doces frescos com até <span>40% OFF</span> para retirada sem fila.`;
    dom.heroSubtitle.textContent =
      'Motor DesperdícioZero ativado! Garanta os itens da última fornada com desconto especial e receba o aviso de retirada direto no seu WhatsApp.';
  } else {
    dom.heroStatusBadge.innerHTML = `${Icons.sparkles} <span>QUEIMA PROGRAMADA ÀS ${horario}</span>`;
    dom.heroTitle.innerHTML = `Peça pelo celular e retire <span>sem fila</span> no intervalo do SENAC.`;
    dom.heroSubtitle.textContent = `Cardápio sincronizado em tempo real com o balcão da Cantina ALAMO. Às ${horario}, os itens perecíveis entram automaticamente em promoção antidesperdício.`;
  }

  const featured = offers[0] || allProducts[0];
  if (featured) {
    dom.showcaseImg.src = resolveProductImage(featured);
    dom.showcaseImg.alt = featured.nome;
    dom.showcaseImg.onerror = () => {
      dom.showcaseImg.src = buildOfflineSvgPlaceholder(featured.nome);
    };
    dom.showcaseTitle.textContent = featured.nome;
    dom.showcaseBadge.textContent = featured.emPromocao
      ? `Oferta ${formatCurrency(featured.precoFinal)}`
      : formatCurrency(featured.precoFinal);
  }
}

function renderCategoryPills() {
  const categories = catalog.categories();
  const selected = catalog.selectedCategoryId();
  const totalProductsCount = catalog.products().length;

  dom.categoryPills.replaceChildren();

  const allBtn = document.createElement('button');
  allBtn.type = 'button';
  allBtn.className = `category-pill${selected === 'all' ? ' active' : ''}`;
  allBtn.innerHTML = `<span>Todos os Itens</span> <span class="category-pill-count">${totalProductsCount}</span>`;
  allBtn.addEventListener('click', () => catalog.selectedCategoryId.set('all'));
  dom.categoryPills.appendChild(allBtn);

  for (const cat of categories) {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = `category-pill${String(selected) === String(cat.id) ? ' active' : ''}`;
    btn.innerHTML = `<span>${cat.nome}</span> <span class="category-pill-count">${cat.totalProdutos}</span>`;
    btn.addEventListener('click', () => catalog.selectedCategoryId.set(cat.id));
    dom.categoryPills.appendChild(btn);
  }
}

function handleCardQuantityDelta(product, delta) {
  cart.setDelta(product, delta);
}

function renderCatalogGrids() {
  const offers = catalog.offers();
  const filtered = catalog.filteredProducts();

  // Vitrine de Ofertas 20:45
  if (offers.length > 0 && catalog.selectedCategoryId() === 'all' && !catalog.searchQuery()) {
    dom.promoSection.hidden = false;
    dom.promoCountPill.textContent = `${offers.length} ${offers.length === 1 ? 'oferta ativa' : 'ofertas ativas'}`;
    dom.promoGrid.replaceChildren();
    offers.forEach((product, idx) => {
      const qty = cart.getQuantity(product.id);
      dom.promoGrid.appendChild(createProductCardElement(product, qty, handleCardQuantityDelta, idx === 0));
    });
  } else {
    dom.promoSection.hidden = true;
  }

  // Grade Principal do Cardápio
  dom.catalogCountPill.textContent = `${filtered.length} ${filtered.length === 1 ? 'item disponível' : 'itens disponíveis'}`;
  dom.catalogGrid.replaceChildren();

  if (filtered.length === 0) {
    const empty = document.createElement('div');
    empty.className = 'empty-state-box';
    empty.innerHTML = `<p>Nenhum produto encontrado para o filtro selecionado.</p>`;
    dom.catalogGrid.appendChild(empty);
    return;
  }

  const prioritizeFirstInCatalog = dom.promoSection.hidden;
  filtered.forEach((product, idx) => {
    const qty = cart.getQuantity(product.id);
    const isLcp = prioritizeFirstInCatalog && idx === 0;
    dom.catalogGrid.appendChild(createProductCardElement(product, qty, handleCardQuantityDelta, isLcp));
  });
}

function renderWhatsAppGroups() {
  const groups = catalog.whatsappGroups();
  dom.whatsappGroupsGrid.replaceChildren();

  for (const g of groups) {
    const link = document.createElement('a');
    link.className = 'wa-group-card';
    link.href = g.linkConvite;
    link.target = '_blank';
    link.rel = 'noopener noreferrer';

    const meta = document.createElement('div');
    meta.className = 'wa-group-meta';
    meta.innerHTML = `<strong>${g.nome}</strong><span>Turno ${g.turno} • ~${g.participantesEstimados} alunos</span>`;

    const badge = document.createElement('span');
    badge.className = 'wa-join-pill';
    badge.innerHTML = `${Icons.messageCircle} <span>Entrar</span>`;

    link.append(meta, badge);
    dom.whatsappGroupsGrid.appendChild(link);
  }
}

function renderCartDrawerAndFloatingBar() {
  const items = cart.items();
  const count = cart.totalItems();
  const subtotal = cart.subtotalOriginal();
  const savings = cart.totalSavings();
  const total = cart.totalFinal();

  dom.navCartBadge.textContent = String(count);
  dom.navCartTotal.textContent = formatCurrency(total);

  if (count > 0) {
    dom.floatingCartPill.hidden = false;
    dom.floatingCartItems.textContent = `${count} ${count === 1 ? 'item na sacola' : 'itens na sacola'}`;
    dom.floatingCartTotal.textContent = formatCurrency(total);
  } else {
    dom.floatingCartPill.hidden = true;
  }

  dom.drawerCartItems.replaceChildren();
  if (items.length === 0) {
    const empty = document.createElement('div');
    empty.className = 'empty-state-box';
    empty.textContent = 'Sua sacola está vazia. Escolha seus lanches favoritos no cardápio!';
    dom.drawerCartItems.appendChild(empty);
    dom.btnSubmitOrder.disabled = true;
  } else {
    dom.btnSubmitOrder.disabled = false;
    for (const entry of items) {
      const row = document.createElement('div');
      row.className = 'cart-item-row';

      const img = document.createElement('img');
      img.src = resolveProductImage(entry.product);
      img.alt = entry.product.nome;
      img.loading = 'lazy';
      img.onerror = () => {
        img.src = buildOfflineSvgPlaceholder(entry.product.nome);
      };

      const info = document.createElement('div');
      info.className = 'cart-item-info';
      const title = document.createElement('strong');
      title.textContent = entry.product.nome;
      const sub = document.createElement('span');
      sub.textContent = `${entry.quantity}x ${formatCurrency(entry.product.precoFinal)} = ${formatCurrency(entry.product.precoFinal * entry.quantity)}`;
      info.append(title, sub);

      const stepper = document.createElement('div');
      stepper.className = 'qty-stepper';
      const minus = document.createElement('button');
      minus.type = 'button';
      minus.textContent = '−';
      minus.addEventListener('click', () => cart.setDelta(entry.product, -1));
      const qty = document.createElement('span');
      qty.textContent = String(entry.quantity);
      const plus = document.createElement('button');
      plus.type = 'button';
      plus.textContent = '+';
      plus.disabled = entry.quantity >= entry.product.quantidadeDisponivel;
      plus.addEventListener('click', () => cart.setDelta(entry.product, 1));
      stepper.append(minus, qty, plus);

      row.append(img, info, stepper);
      dom.drawerCartItems.appendChild(row);
    }
  }

  dom.drawerSubtotal.textContent = formatCurrency(subtotal);
  dom.drawerSavingsRow.hidden = savings <= 0;
  dom.drawerSavings.textContent = `- ${formatCurrency(savings)}`;
  dom.drawerGrandTotal.textContent = formatCurrency(total);
}

/* ==========================================================================
   2. TICKET DIGITAL DE RETIRADA (#AL-XXXX) ESTILO BOARDING PASS
   ========================================================================== */

const STATUS_STEPS = ['Recebido', 'EmPreparo', 'ProntoParaRetirada', 'Concluido'];
const STATUS_LABELS = {
  Recebido: '1. Recebido',
  EmPreparo: '2. Preparo',
  ProntoParaRetirada: '3. Pronto!',
  Concluido: '4. Retirado',
  Cancelado: 'Cancelado'
};

function renderBoardingTicket(pedido) {
  dom.trackerTicketContainer.replaceChildren();
  if (!pedido) return;

  const ticket = document.createElement('div');
  ticket.className = 'boarding-ticket';

  const top = document.createElement('div');
  top.className = 'ticket-top';
  top.innerHTML = `
    <div>
      <div class="ticket-code-label">SENHA FURA-FILA BALCÃO</div>
      <div class="ticket-code-value">${pedido.codigoRetirada}</div>
    </div>
    <span class="ticket-status-pill">${STATUS_LABELS[pedido.status] || pedido.status}</span>
  `;

  const stepper = document.createElement('div');
  stepper.className = 'ticket-stepper';
  const currentIdx = Math.max(0, STATUS_STEPS.indexOf(pedido.status));

  STATUS_STEPS.forEach((stepKey, idx) => {
    const node = document.createElement('div');
    node.className = `step-node${idx <= currentIdx && pedido.status !== 'Cancelado' ? ' active' : ''}`;
    node.innerHTML = `
      <span class="step-dot">${idx + 1}</span>
      <span>${STATUS_LABELS[stepKey].split('. ')[1]}</span>
    `;
    stepper.appendChild(node);
  });

  const body = document.createElement('div');
  body.className = 'ticket-body';

  const clientInfo = document.createElement('div');
  clientInfo.innerHTML = `<strong>Cliente:</strong> ${pedido.nomeCliente} (${pedido.turno}) • <strong>Total:</strong> ${formatCurrency(pedido.valorTotal)}`;
  body.appendChild(clientInfo);

  const itemsList = document.createElement('div');
  itemsList.textContent = (pedido.itens || [])
    .map((i) => `${i.quantidade}x ${i.nomeProduto} (${formatCurrency(i.subtotal)})`)
    .join(' • ');
  body.appendChild(itemsList);

  if (pedido.ultimoLinkWhatsApp) {
    const waBtn = document.createElement('a');
    waBtn.className = 'btn-wa-ticket';
    waBtn.href = pedido.ultimoLinkWhatsApp;
    waBtn.target = '_blank';
    waBtn.rel = 'noopener noreferrer';
    waBtn.innerHTML = `${Icons.messageCircle} <span>Receber / Abrir Comprovante no WhatsApp</span>`;
    body.appendChild(waBtn);
  }

  ticket.append(top, stepper, body);
  dom.trackerTicketContainer.appendChild(ticket);
}

/* ==========================================================================
   3. CONTADOR REGRESSIVO EM TEMPO REAL PARA A QUEIMA DAS 20:45
   ========================================================================== */

function updateCountdownClock() {
  if (catalog.queimaAtivaAgora() || catalog.offers().length > 0) {
    dom.heroCountdownText.textContent = 'Descontos antidesperdício ativos no cardápio!';
    return;
  }

  const targetStr = catalog.horarioProgramadoQueima() || '20:45';
  const [hh, mm] = targetStr.split(':').map((n) => parseInt(n, 10) || 0);
  const now = new Date();
  const target = new Date(now);
  target.setHours(hh, mm, 0, 0);

  if (now >= target) {
    dom.heroCountdownText.textContent = `Janela das ${targetStr} aberta no balcão`;
    return;
  }

  const diffSec = Math.max(0, Math.floor((target.getTime() - now.getTime()) / 1000));
  const hours = String(Math.floor(diffSec / 3600)).padStart(2, '0');
  const mins = String(Math.floor((diffSec % 3600) / 60)).padStart(2, '0');
  const secs = String(diffSec % 60).padStart(2, '0');
  dom.heroCountdownText.textContent = `Próxima Queima (${targetStr}) em ${hours}:${mins}:${secs}`;
}

/* ==========================================================================
   4. FALLBACKS MODERN WEB GUIDANCE (DIALOG LIGHT-DISMISS + USER-INVALID)
   ========================================================================== */

function setupDialogLightDismissFallbacks() {
  const dialogs = document.querySelectorAll('dialog[closedby="any"]');
  if (!('closedBy' in HTMLDialogElement.prototype)) {
    dialogs.forEach((dialog) => {
      dialog.addEventListener('click', (event) => {
        if (event.target !== dialog) return;
        const rect = dialog.getBoundingClientRect();
        const isDialogContent =
          rect.top <= event.clientY &&
          event.clientY <= rect.top + rect.height &&
          rect.left <= event.clientX &&
          event.clientX <= rect.left + rect.width;

        if (isDialogContent) return;
        dialog.close();
      });
    });
  }
}

const UserInvalidFallback = (() => {
  const dirtyState = new WeakMap();

  const updateState = (input) => {
    const isValid = input.checkValidity();
    input.classList.toggle('user-invalid-fallback', !isValid);
    input.classList.toggle('user-valid-fallback', isValid);

    if (!isValid) {
      input.setAttribute('aria-invalid', 'true');
    } else {
      input.removeAttribute('aria-invalid');
    }
  };

  const handleEvent = (event) => {
    const input = event.target;
    if (event.type === 'reset') {
      const controls = input.elements || [];
      for (const control of controls) {
        dirtyState.delete(control);
        control.classList.remove('user-invalid-fallback', 'user-valid-fallback');
        control.removeAttribute('aria-invalid');
      }
      return;
    }

    if (!input?.checkValidity) return;

    if (event.type === 'input' || event.type === 'change') {
      const state = dirtyState.get(input) || { hasInteracted: false, hasBlurred: false };
      state.hasInteracted = true;
      dirtyState.set(input, state);
      if (state.hasBlurred) updateState(input);
    } else if (event.type === 'blur') {
      const state = dirtyState.get(input) || { hasInteracted: false, hasBlurred: false };
      state.hasBlurred = true;
      dirtyState.set(input, state);
      if (state.hasInteracted) updateState(input);
    }
  };

  const init = (root = document) => {
    if (CSS.supports('selector(:user-invalid)')) return;
    root.addEventListener('blur', handleEvent, true);
    root.addEventListener('input', handleEvent);
    root.addEventListener('change', handleEvent);
    root.addEventListener('reset', handleEvent, true);
  };

  return { init };
})();

function setupAriaInvalidSync() {
  const syncAria = (el) => {
    if (!el || typeof el.matches !== 'function') return;
    try {
      el.setAttribute?.('aria-invalid', el.matches(':user-invalid') ? 'true' : 'false');
    } catch {
      // Ignora em navegadores legados sem suporte ao seletor
    }
  };

  document.addEventListener('blur', (e) => syncAria(e.target), true);
  document.addEventListener('input', (e) => {
    if (e.target?.hasAttribute?.('aria-invalid')) syncAria(e.target);
  });
}

/* ==========================================================================
   5. INICIALIZAÇÃO & EVENT LISTENERS
   ========================================================================== */

async function bootstrap() {
  setupDialogLightDismissFallbacks();
  UserInvalidFallback.init(dom.checkoutForm);
  setupAriaInvalidSync();

  // Efeitos Reativos (Signals)
  effect(() => {
    renderHeroAndShowcase();
    renderCategoryPills();
    renderWhatsAppGroups();
  });

  effect(() => {
    renderCatalogGrids();
    renderCartDrawerAndFloatingBar();
  });

  // Filtros
  dom.searchInput.addEventListener('input', (e) => {
    catalog.searchQuery.set(e.target.value || '');
  });

  dom.onlyOffersToggle.addEventListener('change', (e) => {
    catalog.onlyOffers.set(Boolean(e.target.checked));
  });

  // Abrir/Fechar Drawer da Sacola
  const openCartDrawer = () => {
    dom.checkoutFeedback.textContent = '';
    dom.cartDrawerModal.showModal();
  };
  dom.btnNavCart.addEventListener('click', openCartDrawer);
  dom.btnOpenDrawerFloating.addEventListener('click', openCartDrawer);
  dom.btnCloseCartDrawer.addEventListener('click', () => dom.cartDrawerModal.close());

  // Abrir/Fechar Modal de Rastreio de Pedido (#AL-XXXX)
  const openTrackerWithCode = async (code) => {
    dom.trackerModal.showModal();
    if (code) {
      dom.trackerCodeInput.value = code;
      try {
        const pedido = await CantinaApiService.consultarPedido(code);
        renderBoardingTicket(pedido);
      } catch (err) {
        dom.trackerTicketContainer.innerHTML = `<div class="empty-state-box">${err.message}</div>`;
      }
    }
  };

  dom.btnOpenTrackerNav.addEventListener('click', () => openTrackerWithCode(localStorage.getItem('cantina_last_order') || ''));
  dom.btnCloseTrackerModal.addEventListener('click', () => dom.trackerModal.close());

  dom.btnQuickTrack.addEventListener('click', () => {
    const code = dom.quickTrackInput.value.trim();
    openTrackerWithCode(code);
  });

  dom.trackerForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    const code = dom.trackerCodeInput.value.trim();
    if (!code) return;
    try {
      const pedido = await CantinaApiService.consultarPedido(code);
      localStorage.setItem('cantina_last_order', pedido.codigoRetirada);
      renderBoardingTicket(pedido);
    } catch (err) {
      dom.trackerTicketContainer.innerHTML = `<div class="empty-state-box">${err.message}</div>`;
    }
  });

  // Submissão do Checkout Fura-Fila
  dom.checkoutForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (cart.totalItems() <= 0) return;

    dom.btnSubmitOrder.disabled = true;
    dom.checkoutFeedback.textContent = 'Reservando itens no balcão...';

    const payload = {
      nomeCliente: document.getElementById('clienteNome').value.trim(),
      telefoneWhatsApp: document.getElementById('clienteWhatsApp').value.trim(),
      email: document.getElementById('clienteEmail').value.trim() || null,
      matricula: document.getElementById('clienteMatricula').value.trim() || null,
      turno: document.getElementById('clienteTurno').value,
      formaPagamento: document.getElementById('clientePagamento').value,
      observacoes: document.getElementById('clienteObs').value.trim() || null,
      receberAlertasPromocao: document.getElementById('clienteOptIn').checked,
      itens: cart.items().map((entry) => ({
        produtoId: entry.product.id,
        quantidade: entry.quantity
      }))
    };

    try {
      const pedidoCriado = await CantinaApiService.criarPedido(payload);
      localStorage.setItem('cantina_last_order', pedidoCriado.codigoRetirada);
      cart.clear();
      await catalog.loadCatalog(true);
      dom.cartDrawerModal.close();
      dom.checkoutForm.reset();

      dom.trackerCodeInput.value = pedidoCriado.codigoRetirada;
      renderBoardingTicket(pedidoCriado);
      dom.trackerModal.showModal();
    } catch (err) {
      dom.checkoutFeedback.textContent = err.message;
    } finally {
      dom.btnSubmitOrder.disabled = cart.totalItems() <= 0;
    }
  });

  // Carregamento inicial + Polling silencioso sem flicker
  await catalog.loadCatalog(false);
  cart.syncWithCatalog(catalog.products());
  updateCountdownClock();

  setInterval(updateCountdownClock, 1000);
  setInterval(async () => {
    const changed = await catalog.loadCatalog(true);
    if (changed) {
      cart.syncWithCatalog(catalog.products());
    }
  }, 8000);
}

bootstrap();

