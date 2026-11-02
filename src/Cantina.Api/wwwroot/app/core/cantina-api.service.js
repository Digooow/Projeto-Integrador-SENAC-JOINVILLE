/**
 * CAMADA 1 (CORE) — CantinaApiService
 * Gateway HTTP centralizado para comunicação com os endpoints REST do Cantina.Api (.NET 10).
 */

export class CantinaApiService {
  static async getCardapio() {
    const response = await fetch('/api/v1/cardapio', {
      headers: { Accept: 'application/json' }
    });
    if (!response.ok) {
      throw new Error(`Falha ao carregar cardápio (HTTP ${response.status}).`);
    }
    return response.json();
  }

  static async criarPedido(payload) {
    const response = await fetch('/api/v1/pedidos', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json'
      },
      body: JSON.stringify(payload)
    });

    const data = await response.json().catch(() => ({}));
    if (!response.ok) {
      throw new Error(data.erro || 'Não foi possível concluir o pedido agora.');
    }
    return data;
  }

  static async consultarPedido(codigo) {
    const clean = encodeURIComponent(String(codigo ?? '').trim().toUpperCase());
    const response = await fetch(`/api/v1/pedidos/${clean}`, {
      headers: { Accept: 'application/json' }
    });

    const data = await response.json().catch(() => ({}));
    if (!response.ok) {
      throw new Error(data.erro || 'Pedido não encontrado. Verifique o código informado.');
    }
    return data;
  }
}

