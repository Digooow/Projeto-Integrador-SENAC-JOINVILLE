namespace Cantina.Domain.Enums;

public enum TurnoSenac
{
    Manha = 1,
    Tarde = 2,
    Noite = 3
}

public enum OrigemPedido
{
    SiteWeb = 1,
    BalcaoDesktop = 2
}

public enum StatusPedido
{
    Recebido = 1,
    EmPreparo = 2,
    ProntoParaRetirada = 3,
    Concluido = 4,
    Cancelado = 5
}

public enum FormaPagamento
{
    PixCopiaECola = 1,
    PixNaRetirada = 2,
    CartaoNoBalcao = 3,
    DinheiroNoBalcao = 4
}

public enum TipoMovimentacaoEstoque
{
    EntradaProducao = 1,
    ReservaPedido = 2,
    VendaBalcao = 3,
    BaixaEntregaPedido = 4,
    EstornoCancelamento = 5,
    PerdaDescarte = 6,
    AjusteManual = 7
}

public enum TipoNotificacaoWhatsApp
{
    ConfirmacaoPedido = 1,
    PedidoEmPreparo = 2,
    PedidoProntoRetirada = 3,
    PedidoCancelado = 4,
    CampanhaQueimaEstoque = 5
}

public enum CanalEnvioWhatsApp
{
    MetaCloudApi = 1,
    EvolutionApi = 2,
    OutboxWaMeFallback = 3
}

