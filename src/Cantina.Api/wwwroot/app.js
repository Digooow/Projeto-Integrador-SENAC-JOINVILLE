(() => {
  const fmtMoeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  let cardapio = { categorias: [], produtos: [], gruposWhatsApp: [] };
  let categoriaSelecionada = 'TODOS';
  let termoBusca = '';
  const sacola = new Map(); // produtoId -> quantidade
  let trackerTimer = null;

  const checkoutDialog = document.getElementById('checkoutDialog');
  const trackerDialog = document.getElementById('trackerDialog');

  // Fallback obrigatório para navegadores sem suporte nativo a <dialog closedby="any">
  const attachLightDismissFallback = (dialog) => {
    if (!dialog) return;
    if (!('closedBy' in HTMLDialogElement.prototype)) {
      dialog.addEventListener('click', (event) => {
        if (event.target !== dialog) return;
        const rect = dialog.getBoundingClientRect();
        const isDialogContent = (
          rect.top <= event.clientY &&
          event.clientY <= rect.top + rect.height &&
          rect.left <= event.clientX &&
          event.clientX <= rect.left + rect.width
        );
        if (isDialogContent) return;
        dialog.close();
      });
    }
  };

  attachLightDismissFallback(checkoutDialog);
  attachLightDismissFallback(trackerDialog);

  // Sincronização de aria-invalid com :user-invalid
  const syncAria = (el) => {
    if (!el || typeof el.matches !== 'function') return;
    try {
      el.setAttribute('aria-invalid', el.matches(':user-invalid') ? 'true' : 'false');
    } catch {
      // Ignora seletores não aplicáveis ao alvo
    }
  };

  document.addEventListener('blur', (e) => syncAria(e.target), true);
  document.addEventListener('input', (e) => {
    if (e.target && e.target.hasAttribute && e.target.hasAttribute('aria-invalid')) {
      syncAria(e.target);
    }
  });

  // Fechamento de modais via data-close-dialog
  document.querySelectorAll('[data-close-dialog]').forEach((btn) => {
    btn.addEventListener('click', () => {
      const id = btn.getAttribute('data-close-dialog');
      document.getElementById(id)?.close();
    });
  });

  // Abertura do carrinho pelo botão do topo
  document.getElementById('btnTopoCarrinho')?.addEventListener('click', () => {
    const { qtdItens } = calcularTotaisSacola();
    if (qtdItens > 0) {
      abrirModalCheckout();
    } else {
      // Se a sacola estiver vazia, rola suavemente para o cardápio
      document.querySelector('.toolbar-section')?.scrollIntoView({ behavior: 'smooth' });
    }
  });

  // Botão do Hero para filtrar ofertas
  document.getElementById('btnVerOfertasHero')?.addEventListener('click', () => {
    categoriaSelecionada = 'OFERTAS';
    renderizarCategorias();
    renderizarProdutos();
    document.querySelector('.toolbar-section')?.scrollIntoView({ behavior: 'smooth' });
  });

  // Limpar busca
  const campoBusca = document.getElementById('campoBusca');
  const btnLimparBusca = document.getElementById('btnLimparBusca');

  campoBusca?.addEventListener('input', (e) => {
    termoBusca = (e.target.value || '').trim().toLowerCase();
    btnLimparBusca.hidden = !termoBusca;
    renderizarProdutos();
  });

  btnLimparBusca?.addEventListener('click', () => {
    campoBusca.value = '';
    termoBusca = '';
    btnLimparBusca.hidden = true;
    renderizarProdutos();
    campoBusca.focus();
  });

  async function carregarCardapio() {
    try {
      const resp = await fetch('/api/v1/cardapio');
      if (!resp.ok) return;
      cardapio = await resp.json();

      document.getElementById('cantinaNome').textContent = cardapio.nomeCantina;
      document.getElementById('horarioQueimaTexto').textContent = `Queima diária às ${cardapio.horarioQueima}`;

      const ofertasAtivas = cardapio.produtos.filter((p) => p.temDesconto && p.quantidadeDisponivel > 0);
      document.getElementById('contadorOfertasHero').textContent = String(ofertasAtivas.length);

      const badge = document.getElementById('badgeQueima');
      if (cardapio.modoAntidesperdicioAtivo) {
        badge.className = 'burn-pill active-burn';
        badge.innerHTML = `<span class="burn-icon" aria-hidden="true">🔥</span><span>Cardápio Antidesperdício ATIVO! (${ofertasAtivas.length} ofertas)</span>`;
      } else {
        badge.className = 'burn-pill';
        badge.innerHTML = `<span class="burn-icon" aria-hidden="true">🔥</span><span id="horarioQueimaTexto">Queima diária às ${cardapio.horarioQueima}</span>`;
      }

      renderizarGrupos();
      renderizarCategorias();
      renderizarProdutos();
      atualizarBarraSacola();
    } catch (err) {
      console.error('Erro ao carregar cardápio:', err);
    }
  }

  function renderizarGrupos() {
    const container = document.getElementById('gruposContainer');
    container.innerHTML = '';

    cardapio.gruposWhatsApp.forEach((g) => {
      const link = document.createElement('a');
      link.className = 'group-chip';
      link.href = g.linkConvite || '#';
      link.target = '_blank';
      link.rel = 'noopener noreferrer';
      link.innerHTML = `<span aria-hidden="true">💬</span> <span>Grupo ${g.nome}</span> <span style="opacity:0.75; font-size:0.76rem;">(${g.participantesEstimados})</span>`;
      container.appendChild(link);
    });
  }

  function renderizarCategorias() {
    const container = document.getElementById('categoriasContainer');
    container.innerHTML = '';

    const opcoes = [
      { id: 'TODOS', label: '🍽️ Todos' },
      { id: 'OFERTAS', label: '🔥 Ofertas 20:45' },
      ...cardapio.categorias.map((c) => ({ id: String(c.id), label: `${c.iconeEmoji} ${c.nome}` }))
    ];

    opcoes.forEach((op) => {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.setAttribute('data-cat', op.id);
      btn.className = `pill-btn ${categoriaSelecionada === op.id ? 'active' : ''}`;
      btn.textContent = op.label;
      btn.addEventListener('click', () => {
        categoriaSelecionada = op.id;
        renderizarCategorias();
        renderizarProdutos();
      });
      container.appendChild(btn);
    });
  }

  function renderizarProdutos() {
    const grid = document.getElementById('catalogoGrid');
    grid.innerHTML = '';

    const filtrados = cardapio.produtos.filter((p) => {
      if (categoriaSelecionada === 'OFERTAS' && !p.temDesconto) return false;
      if (categoriaSelecionada !== 'TODOS' && categoriaSelecionada !== 'OFERTAS' && String(p.categoriaId) !== categoriaSelecionada) {
        return false;
      }
      if (termoBusca && !p.nome.toLowerCase().includes(termoBusca) && !p.descricao.toLowerCase().includes(termoBusca)) {
        return false;
      }
      return true;
    });

    if (filtrados.length === 0) {
      const emptyBox = document.createElement('div');
      emptyBox.className = 'tracker-empty-state';
      emptyBox.style.gridColumn = '1 / -1';
      emptyBox.innerHTML = `
        <span class="empty-icon">🔍</span>
        <strong>Nenhum produto encontrado</strong>
        <p style="margin:4px 0 0; font-size:0.86rem; color:var(--slate-500);">Tente buscar outro termo ou selecione outra categoria.</p>
      `;
      grid.appendChild(emptyBox);
      return;
    }

    filtrados.forEach((p) => {
      const qtdNaSacola = sacola.get(p.id) || 0;
      const esgotado = p.quantidadeDisponivel <= 0;

      const card = document.createElement('article');
      card.className = `product-card ${p.temDesconto ? 'promo' : ''}`;

      // Badge de Promoção
      if (p.temDesconto) {
        const ribbon = document.createElement('span');
        ribbon.className = 'promo-ribbon';
        ribbon.innerHTML = `🔥 -${p.percentualDescontoEfetivo}% OFF`;
        card.appendChild(ribbon);
      }

      // Conteúdo Superior
      const body = document.createElement('div');

      const top = document.createElement('div');
      top.className = 'product-top';

      const emoji = document.createElement('div');
      emoji.className = 'product-emoji';
      emoji.textContent = p.iconeEmoji || '🥐';

      const meta = document.createElement('div');
      meta.className = 'product-meta';

      const catTag = document.createElement('span');
      catTag.className = 'product-cat-tag';
      catTag.textContent = p.categoriaNome;

      const title = document.createElement('h4');
      title.className = 'product-title';
      title.textContent = p.nome;

      meta.append(catTag, title);
      top.append(emoji, meta);

      const desc = document.createElement('p');
      desc.className = 'product-desc';
      desc.textContent = p.descricao || 'Preparado fresquinho na Cantina ALAMO.';

      body.append(top, desc);

      // Rodapé com Preço e Ação
      const footer = document.createElement('div');
      footer.className = 'product-footer';

      const priceCol = document.createElement('div');
      priceCol.className = 'product-pricing';

      if (p.temDesconto) {
        const oldRow = document.createElement('div');
        oldRow.className = 'price-old-row';

        const oldP = document.createElement('span');
        oldP.className = 'price-old';
        oldP.textContent = `De ${fmtMoeda.format(p.precoVenda)}`;

        const savingTag = document.createElement('span');
        savingTag.className = 'badge-saving-pill';
        savingTag.textContent = `-${fmtMoeda.format(p.economiaUnitaria)}`;

        oldRow.append(oldP, savingTag);
        priceCol.appendChild(oldRow);
      }

      const finalP = document.createElement('span');
      finalP.className = 'price-final';
      finalP.textContent = fmtMoeda.format(p.precoFinal);

      const stock = document.createElement('span');
      if (esgotado) {
        stock.className = 'stock-pill out';
        stock.textContent = '❌ Esgotado hoje';
      } else if (p.quantidadeDisponivel <= 3) {
        stock.className = 'stock-pill low';
        stock.textContent = `⚠️ Só restam ${p.quantidadeDisponivel}!`;
      } else {
        stock.className = 'stock-pill';
        stock.textContent = `✓ ${p.quantidadeDisponivel} disponíveis`;
      }

      priceCol.append(finalP, stock);

      const actionCol = document.createElement('div');
      if (qtdNaSacola === 0) {
        const addBtn = document.createElement('button');
        addBtn.type = 'button';
        addBtn.className = 'btn-card-add';
        addBtn.disabled = esgotado;
        addBtn.innerHTML = esgotado ? 'Esgotado' : '<span aria-hidden="true">+</span> Pedir';
        addBtn.addEventListener('click', () => alterarSacola(p.id, 1));
        actionCol.appendChild(addBtn);
      } else {
        const stepper = document.createElement('div');
        stepper.className = 'stepper-container';

        const minus = document.createElement('button');
        minus.type = 'button';
        minus.className = 'stepper-btn';
        minus.textContent = '−';
        minus.setAttribute('aria-label', `Remover uma unidade de ${p.nome}`);
        minus.addEventListener('click', () => alterarSacola(p.id, -1));

        const count = document.createElement('span');
        count.className = 'stepper-count';
        count.textContent = String(qtdNaSacola);

        const plus = document.createElement('button');
        plus.type = 'button';
        plus.className = 'stepper-btn';
        plus.textContent = '+';
        plus.setAttribute('aria-label', `Adicionar uma unidade de ${p.nome}`);
        plus.disabled = qtdNaSacola >= p.quantidadeDisponivel;
        plus.addEventListener('click', () => alterarSacola(p.id, 1));

        stepper.append(minus, count, plus);
        actionCol.appendChild(stepper);
      }

      footer.append(priceCol, actionCol);
      card.append(body, footer);
      grid.appendChild(card);
    });
  }

  function alterarSacola(produtoId, delta) {
    const prod = cardapio.produtos.find((x) => x.id === produtoId);
    if (!prod) return;

    const atual = sacola.get(produtoId) || 0;
    const novo = Math.max(0, Math.min(prod.quantidadeDisponivel, atual + delta));

    if (novo === 0) {
      sacola.delete(produtoId);
    } else {
      sacola.set(produtoId, novo);
    }

    renderizarProdutos();
    atualizarBarraSacola();
    if (checkoutDialog.open) {
      preencherResumoModalCheckout();
    }
  }

  function calcularTotaisSacola() {
    let qtdItens = 0;
    let totalFinal = 0;
    let economia = 0;

    for (const [id, qtd] of sacola.entries()) {
      const p = cardapio.produtos.find((x) => x.id === id);
      if (!p) continue;
      qtdItens += qtd;
      totalFinal += qtd * p.precoFinal;
      economia += qtd * p.economiaUnitaria;
    }

    return { qtdItens, totalFinal, economia };
  }

  function atualizarBarraSacola() {
    const barra = document.getElementById('barraSacola');
    const badgeTopo = document.getElementById('badgeContadorTopo');
    const { qtdItens, totalFinal, economia } = calcularTotaisSacola();

    if (badgeTopo) {
      badgeTopo.textContent = String(qtdItens);
    }

    if (qtdItens === 0) {
      barra.hidden = true;
      return;
    }

    barra.hidden = false;
    document.getElementById('sacolaBadgeQtd').textContent = String(qtdItens);
    document.getElementById('sacolaResumoItens').textContent =
      `${qtdItens} ${qtdItens === 1 ? 'item selecionado' : 'itens selecionados'}`;

    const ecoBadge = document.getElementById('sacolaResumoEconomia');
    if (economia > 0) {
      ecoBadge.hidden = false;
      ecoBadge.textContent = `Economia: ${fmtMoeda.format(economia)}`;
    } else {
      ecoBadge.hidden = true;
    }

    document.getElementById('sacolaResumoValores').textContent =
      `Total: ${fmtMoeda.format(totalFinal)}`;
  }

  function abrirModalCheckout() {
    document.getElementById('erroEnvioPedido').hidden = true;
    preencherResumoModalCheckout();
    checkoutDialog.showModal();
  }

  document.getElementById('btnFinalizarSacola')?.addEventListener('click', abrirModalCheckout);

  function preencherResumoModalCheckout() {
    const lista = document.getElementById('itensSacolaLista');
    const totaisBox = document.getElementById('totaisSacolaBox');
    const counterBadge = document.getElementById('checkoutTotalItensCount');
    lista.innerHTML = '';

    const { qtdItens, totalFinal, economia } = calcularTotaisSacola();
    if (counterBadge) {
      counterBadge.textContent = `${qtdItens} ${qtdItens === 1 ? 'item' : 'itens'}`;
    }

    for (const [id, qtd] of sacola.entries()) {
      const p = cardapio.produtos.find((x) => x.id === id);
      if (!p) continue;

      const row = document.createElement('div');
      row.className = 'cart-item-row';

      const left = document.createElement('div');
      left.className = 'cart-item-left';

      const em = document.createElement('span');
      em.className = 'cart-item-emoji';
      em.textContent = p.iconeEmoji || '🥐';

      const info = document.createElement('div');
      info.className = 'cart-item-info';

      const name = document.createElement('strong');
      name.textContent = `${qtd}x ${p.nome}`;

      const unit = document.createElement('span');
      unit.textContent = `${fmtMoeda.format(p.precoFinal)} cada`;

      info.append(name, unit);
      left.append(em, info);

      const subtotal = document.createElement('span');
      subtotal.className = 'cart-item-subtotal';
      subtotal.textContent = fmtMoeda.format(qtd * p.precoFinal);

      row.append(left, subtotal);
      lista.appendChild(row);
    }

    totaisBox.innerHTML = `
      ${economia > 0 ? `
        <div class="cart-total-row saving">
          <span>🌱 Economia Antidesperdício:</span>
          <strong>-${fmtMoeda.format(economia)}</strong>
        </div>
      ` : ''}
      <div class="cart-total-row main-total">
        <span>Total a Pagar na Retirada:</span>
        <strong style="color:var(--emerald-600);">${fmtMoeda.format(totalFinal)}</strong>
      </div>
    `;
  }

  document.getElementById('formPedido')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const form = e.currentTarget;
    if (!form.reportValidity()) return;

    const btn = document.getElementById('btnEnviarPedido');
    const erroBox = document.getElementById('erroEnvioPedido');
    erroBox.hidden = true;
    btn.disabled = true;
    btn.innerHTML = '<span>Reservando pedido...</span>';

    try {
      const itens = Array.from(sacola.entries()).map(([produtoId, quantidade]) => ({
        produtoId,
        quantidade
      }));

      const payload = {
        nomeCliente: document.getElementById('clienteNome').value.trim(),
        telefoneWhatsApp: document.getElementById('clienteWhatsApp').value.trim(),
        matriculaAluno: document.getElementById('clienteMatricula').value.trim() || null,
        turno: document.getElementById('clienteTurno').value,
        formaPagamento: document.getElementById('clientePagamento').value,
        observacoes: document.getElementById('clienteObs').value.trim() || null,
        itens
      };

      const resp = await fetch('/api/v1/pedidos', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      const body = await resp.json();
      if (!resp.ok) {
        erroBox.textContent = body.detail || 'Não foi possível concluir o pedido.';
        erroBox.hidden = false;
        return;
      }

      sacola.clear();
      localStorage.setItem('ultimoPedidoCantina', body.codigoRetirada);
      checkoutDialog.close();
      await carregarCardapio();
      abrirTrackerComCodigo(body.codigoRetirada, body);
    } catch {
      erroBox.textContent = 'Falha de conexão com o servidor da cantina.';
      erroBox.hidden = false;
    } finally {
      btn.disabled = false;
      btn.innerHTML = '<span class="btn-icon">⚡</span><span>Confirmar Pedido Fura-Fila</span>';
    }
  });

  document.getElementById('btnAbrirRastreador')?.addEventListener('click', () => {
    const salvo = localStorage.getItem('ultimoPedidoCantina') || '';
    if (salvo) {
      abrirTrackerComCodigo(salvo);
    } else {
      trackerDialog.showModal();
    }
  });

  document.getElementById('formBuscaCodigo')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const cod = document.getElementById('inputCodigoPedido').value.trim();
    if (cod) {
      await consultarPedido(cod);
    }
  });

  trackerDialog?.addEventListener('close', () => {
    if (trackerTimer) {
      clearInterval(trackerTimer);
      trackerTimer = null;
    }
  });

  function abrirTrackerComCodigo(codigo, dadosIniciais = null) {
    document.getElementById('inputCodigoPedido').value = codigo;
    if (!trackerDialog.open) {
      trackerDialog.showModal();
    }
    if (dadosIniciais) {
      renderizarStatusPedido(dadosIniciais);
    } else {
      consultarPedido(codigo);
    }

    if (trackerTimer) clearInterval(trackerTimer);
    trackerTimer = setInterval(() => consultarPedido(codigo), 4000);
  }

  async function consultarPedido(codigo) {
    const container = document.getElementById('resultadoTracker');
    try {
      const resp = await fetch(`/api/v1/pedidos/${encodeURIComponent(codigo.replace('#', ''))}`);
      if (!resp.ok) {
        container.innerHTML = `
          <div class="tracker-empty-state">
            <span class="empty-icon">❌</span>
            <strong>Pedido não localizado</strong>
            <p style="margin:4px 0 0; font-size:0.86rem; color:var(--slate-500);">Verifique se o código "${codigo}" foi digitado corretamente.</p>
          </div>
        `;
        return;
      }

      const pedido = await resp.json();
      localStorage.setItem('ultimoPedidoCantina', pedido.codigoRetirada);
      renderizarStatusPedido(pedido);
    } catch {
      // Mantém último estado visível durante oscilações de rede
    }
  }

  function renderizarStatusPedido(pedido) {
    const container = document.getElementById('resultadoTracker');
    container.innerHTML = '';

    const card = document.createElement('div');
    card.className = 'pickup-card';

    const meta = document.createElement('div');
    meta.className = 'pickup-header-meta';
    meta.textContent = `Cliente: ${pedido.nomeCliente} • Turno ${pedido.turno}`;

    const code = document.createElement('div');
    code.className = 'pickup-code';
    code.textContent = `#${pedido.codigoRetirada}`;

    const etapas = [
      { key: 'Recebido', texto: '1. Recebido' },
      { key: 'EmPreparo', texto: '2. Em Preparo' },
      { key: 'ProntoParaRetirada', texto: '3. Pronto!' },
      { key: 'Concluido', texto: '4. Retirado' }
    ];

    const stepsDiv = document.createElement('div');
    stepsDiv.className = 'status-steps';

    if (pedido.status === 'Cancelado') {
      const cancelMsg = document.createElement('div');
      cancelMsg.className = 'alert-error';
      cancelMsg.textContent = 'Este pedido foi cancelado pela cantina.';
      card.append(meta, code, cancelMsg);
    } else {
      etapas.forEach((et) => {
        const b = document.createElement('div');
        const isCurrent = pedido.status === et.key;
        const isReady = isCurrent && (et.key === 'ProntoParaRetirada' || et.key === 'Concluido');
        b.className = `step-badge ${isReady ? 'ready' : (isCurrent ? 'active' : '')}`;
        b.textContent = et.texto;
        stepsDiv.appendChild(b);
      });

      const msgStatus = document.createElement('p');
      msgStatus.className = pedido.status === 'ProntoParaRetirada' ? 'tracker-msg ready-alert' : 'tracker-msg';
      msgStatus.textContent = pedido.status === 'ProntoParaRetirada'
        ? '🔔 SEU LANCHE ESTÁ PRONTO! Passe direto no balcão Fura-Fila informando o código acima.'
        : 'Seu pedido foi registrado. Avisaremos no WhatsApp quando estiver pronto para retirada!';

      card.append(meta, code, stepsDiv, msgStatus);
    }

    if (pedido.ultimoLinkWhatsApp) {
      const waLink = document.createElement('a');
      waLink.className = 'group-chip';
      waLink.href = pedido.ultimoLinkWhatsApp;
      waLink.target = '_blank';
      waLink.rel = 'noopener noreferrer';
      waLink.style.display = 'inline-flex';
      waLink.style.marginTop = '12px';
      waLink.innerHTML = '📲 Abrir conversa no WhatsApp';
      card.appendChild(waLink);
    }

    container.appendChild(card);
  }

  // Consulta por query param ?pedido=AL-XXXX
  const params = new URLSearchParams(window.location.search);
  const pedidoQuery = params.get('pedido');
  carregarCardapio().then(() => {
    if (pedidoQuery) {
      abrirTrackerComCodigo(pedidoQuery);
    }
  });

  // Atualização suave a cada 10s
  setInterval(carregarCardapio, 10000);
})();
