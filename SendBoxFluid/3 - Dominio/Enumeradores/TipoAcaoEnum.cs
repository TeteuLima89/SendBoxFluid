namespace SendBoxFluid.Dominio.Enumeradores;

/// <summary>
/// Identifica o tipo de acao que o fluxo Fluid esta executando,
/// inferido pelo endpoint chamado no SendBox.
/// </summary>
public enum TipoAcaoEnum
{
    Desconhecido = 0,
    Login = 1,
    NotaFiscalEntradaDraft = 2,
    NotaFiscalEntradaRecebimento = 3,
    NotaFiscalSaida = 4,
    NotaFiscalTransito = 5,
    DespesaImportacao = 6,
    LancamentoContabil = 7,
    Adiantamento = 8,
    Pagamento = 9,
    ConsultaPedidoCompra = 10,
    ConsultaPedidoVenda = 11,
    ConsultaNotaFiscal = 12,
    Cancelamento = 13,
    Atualizacao = 14,
    PedidoVenda = 15,
    PedidoCompra = 16,
    TransferenciaEstoque = 17
}
