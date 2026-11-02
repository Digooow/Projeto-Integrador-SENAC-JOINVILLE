/**
 * CAMADA 1 (CORE) — Contratos TypeScript espelhados de src/Cantina.Contracts/Dtos/CantinaContracts.cs
 * Qualquer alteração nos DTOs do backend .NET 10 deve ser refletida aqui.
 */

export interface CategoriaDto {
  readonly id: number;
  readonly nome: string;
  readonly iconeEmoji: string;
  readonly ordemExibicao: number;
  readonly totalProdutos: number;
}

export interface ProdutoDto {
  readonly id: number;
  readonly nome: string;
  readonly descricao: string;
  readonly iconeEmoji: string;
  readonly categoriaId: number;
  readonly categoriaNome: string;
  readonly precoCusto: number;
  readonly precoVenda: number;
  readonly precoFinal: number;
  readonly percentualDescontoEfetivo: number;
  readonly descontoPercentual: number;
  readonly descontoValorFixo: number;
  readonly emPromocao: boolean;
  readonly perecivel: boolean;
  readonly ativo: boolean;
  readonly quantidadeEstoque: number;
  readonly quantidadeReservada: number;
  readonly quantidadeDisponivel: number;
  readonly imagemUrl?: string;
}

export interface GrupoWhatsAppDto {
  readonly id: number;
  readonly nome: string;
  readonly turno: 'Manha' | 'Tarde' | 'Noite' | string;
  readonly linkConvite: string;
  readonly destinoApiJid: string | null;
  readonly participantesEstimados: number;
  readonly ativo: boolean;
}

export interface CardapioPublicoDto {
  readonly nomeCantina: string;
  readonly slogan: string;
  readonly queimaAtivaAgora: boolean;
  readonly horarioProgramadoQueima: string;
  readonly categorias: readonly CategoriaDto[];
  readonly produtos: readonly ProdutoDto[];
  readonly ofertasDoDia: readonly ProdutoDto[];
  readonly gruposWhatsApp: readonly GrupoWhatsAppDto[];
}

export interface CriarItemPedidoRequest {
  readonly produtoId: number;
  readonly quantidade: number;
}

export interface CriarPedidoWebRequest {
  readonly nomeCliente: string;
  readonly telefoneWhatsApp: string;
  readonly email?: string | null;
  readonly matricula?: string | null;
  readonly turno: string;
  readonly formaPagamento: string;
  readonly observacoes?: string | null;
  readonly receberAlertasPromocao: boolean;
  readonly itens: readonly CriarItemPedidoRequest[];
}

export interface ItemPedidoDto {
  readonly id: number;
  readonly produtoId: number;
  readonly nomeProduto: string;
  readonly iconeEmoji: string;
  readonly quantidade: number;
  readonly precoOriginalUnitario: number;
  readonly precoUnitarioCobrado: number;
  readonly subtotal: number;
  readonly vendidoEmPromocao: boolean;
}

export interface PedidoDto {
  readonly id: number;
  readonly codigoRetirada: string;
  readonly status: 'Recebido' | 'EmPreparo' | 'ProntoParaRetirada' | 'Concluido' | 'Cancelado';
  readonly origem: 'Web' | 'Balcao';
  readonly formaPagamento: string;
  readonly turno: string;
  readonly nomeCliente: string;
  readonly telefoneWhatsApp: string;
  readonly matricula: string | null;
  readonly observacoes: string | null;
  readonly valorSubtotalOriginal: number;
  readonly valorDescontoTotal: number;
  readonly valorTotal: number;
  readonly receitaRecuperada: number;
  readonly criadoEm: string;
  readonly prontoEm: string | null;
  readonly concluidoEm: string | null;
  readonly ultimoLinkWhatsApp: string | null;
  readonly itens: readonly ItemPedidoDto[];
}

export interface CartItem {
  readonly product: ProdutoDto;
  readonly quantity: number;
}

