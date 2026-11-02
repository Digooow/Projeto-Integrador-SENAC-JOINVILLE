import { ProdutoDto } from '../models/cantina.models';

/**
 * CAMADA 1 (CORE) — Resolvedor de Fotografia Gastronômica Real (Zero-Emoji Policy)
 * Mapeia produtos e categorias da Cantina ALAMO para fotografias culinárias reais em alta resolução (proporção 4:3).
 */
export class ProductImageResolver {
  private static readonly KEYWORD_IMAGES: ReadonlyArray<{ keywords: readonly string[]; url: string }> = [
    {
      keywords: ['coxinha', 'frango com catupiry', 'salgado frito'],
      url: 'https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?auto=format&fit=crop&w=800&q=80'
    },
    {
      keywords: ['pão de queijo', 'pao de queijo', 'queijo', 'mineiro'],
      url: 'https://images.unsplash.com/photo-1598142919915-6c301c222d04?auto=format&fit=crop&w=800&q=80'
    },
    {
      keywords: ['croissant', 'folhado', 'presunto', 'manteiga'],
      url: 'https://images.unsplash.com/photo-1555507036-ab1f4038808a?auto=format&fit=crop&w=800&q=80'
    },
    {
      keywords: ['pastel', 'assado', 'forno', 'esfiha', 'empada', 'enroladinho'],
      url: 'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80'
    },
    {
      keywords: ['sanduíche', 'sanduiche', 'natural', 'integral', 'lanche', 'peito de peru', 'wrap', 'baguete'],
      url: 'https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80'
    },
    {
      keywords: ['hambúrguer', 'hamburguer', 'burger', 'artesanal'],
      url: 'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&q=80'
    },
    {
      keywords: ['suco', 'laranja', 'vitamina', 'natural', 'limonada', 'maracujá'],
      url: 'https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=800&q=80'
    },
    {
      keywords: ['café', 'cafe', 'cappuccino', 'espresso', 'expresso', 'chocolate quente', 'mocaccino'],
      url: 'https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&w=800&q=80'
    },
    {
      keywords: ['refrigerante', 'coca', 'guaraná', 'guarana', 'lata', 'água', 'agua', 'chá', 'cha'],
      url: 'https://images.unsplash.com/photo-1581006852262-e4307cf6283a?auto=format&fit=crop&w=800&q=80'
    },
    {
      keywords: ['brownie', 'bolo', 'chocolate', 'brigadeiro', 'cookie', 'torta', 'doce', 'pudim'],
      url: 'https://images.unsplash.com/photo-1606313564200-e75d5e30476c?auto=format&fit=crop&w=800&q=80'
    },
    {
      keywords: ['açaí', 'acai', 'salada de frutas', 'iogurte'],
      url: 'https://images.unsplash.com/photo-1590301157890-4810ed352733?auto=format&fit=crop&w=800&q=80'
    }
  ];

  private static readonly CATEGORY_FALLBACKS: Readonly<Record<string, string>> = {
    salgados: 'https://images.unsplash.com/photo-1555507036-ab1f4038808a?auto=format&fit=crop&w=800&q=80',
    lanches: 'https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80',
    bebidas: 'https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=800&q=80',
    doces: 'https://images.unsplash.com/photo-1606313564200-e75d5e30476c?auto=format&fit=crop&w=800&q=80'
  };

  public static readonly DEFAULT_FOOD_IMAGE =
    'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80';

  public static resolve(product: Pick<ProdutoDto, 'nome' | 'descricao' | 'categoriaNome' | 'imagemUrl'>): string {
    if (product.imagemUrl && product.imagemUrl.trim().startsWith('http')) {
      return product.imagemUrl.trim();
    }

    const haystack = `${product.nome ?? ''} ${product.descricao ?? ''}`.toLowerCase();
    for (const entry of ProductImageResolver.KEYWORD_IMAGES) {
      if (entry.keywords.some((kw) => haystack.includes(kw))) {
        return entry.url;
      }
    }

    const catLower = (product.categoriaNome ?? '').toLowerCase();
    for (const [key, url] of Object.entries(ProductImageResolver.CATEGORY_FALLBACKS)) {
      if (catLower.includes(key)) {
        return url;
      }
    }

    return ProductImageResolver.DEFAULT_FOOD_IMAGE;
  }
}

