using SendBoxFluid.Dominio.Enumeradores;

namespace SendBoxFluid.Dominio.Servicos;

/// <summary>
/// Identifica que tipo de acao do fluxo Fluid esta sendo executada,
/// olhando pro endpoint chamado e pro corpo da requisicao.
/// </summary>
public static class ServicoIdentificadorAcao
{
    private static readonly (string Fragmento, TipoAcaoEnum Acao)[] _mapaPost = new (string, TipoAcaoEnum)[]
    {
        ("StockTransfers",        TipoAcaoEnum.TransferenciaEstoque),
        ("PurchaseOrders",        TipoAcaoEnum.PedidoCompra),
        ("PurchaseDeliveryNotes", TipoAcaoEnum.NotaFiscalEntradaRecebimento),
        ("PurchaseDownPayments",  TipoAcaoEnum.Adiantamento),
        ("VendorPayments",        TipoAcaoEnum.Pagamento),
        ("LandedCosts",           TipoAcaoEnum.DespesaImportacao),
        ("JournalEntries",        TipoAcaoEnum.LancamentoContabil),
        ("Orders",                TipoAcaoEnum.PedidoVenda),
    };

    public static TipoAcaoEnum Identificar(string metodo, string caminho, string corpoRequisicao)
    {
        if (caminho.Contains("/Login", StringComparison.OrdinalIgnoreCase))
            return TipoAcaoEnum.Login;

        if (metodo.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
            return TipoAcaoEnum.Atualizacao;

        if (caminho.Contains("/Cancel", StringComparison.OrdinalIgnoreCase))
            return TipoAcaoEnum.Cancelamento;

        if (metodo.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            if (caminho.Contains("PurchaseOrders", StringComparison.OrdinalIgnoreCase))
                return TipoAcaoEnum.ConsultaPedidoCompra;
            if (caminho.Contains("/Orders", StringComparison.OrdinalIgnoreCase))
                return TipoAcaoEnum.ConsultaPedidoVenda;
            if (caminho.Contains("PurchaseInvoices", StringComparison.OrdinalIgnoreCase))
                return TipoAcaoEnum.ConsultaNotaFiscal;
            return TipoAcaoEnum.Desconhecido;
        }

        // POST - Drafts inspeciona o body pelo DocObjectCode antes do mapa
        if (caminho.Contains("/Drafts", StringComparison.OrdinalIgnoreCase))
        {
            if (corpoRequisicao.Contains("\"DocObjectCode\":\"18\"") || corpoRequisicao.Contains("\"DocObjectCode\": \"18\""))
                return TipoAcaoEnum.NotaFiscalEntradaDraft;
            if (corpoRequisicao.Contains("\"DocObjectCode\":\"13\"") || corpoRequisicao.Contains("\"DocObjectCode\": \"13\""))
                return TipoAcaoEnum.NotaFiscalSaida;
            if (corpoRequisicao.Contains("\"BaseType\":18") || corpoRequisicao.Contains("\"BaseType\": 18"))
                return TipoAcaoEnum.NotaFiscalTransito;
            return TipoAcaoEnum.NotaFiscalEntradaDraft;
        }

        foreach (var (fragmento, acao) in _mapaPost)
        {
            if (caminho.Contains(fragmento, StringComparison.OrdinalIgnoreCase))
                return acao;
        }

        return TipoAcaoEnum.Desconhecido;
    }

    public static string ObterDescricao(TipoAcaoEnum tipo) => tipo switch
    {
        TipoAcaoEnum.Login                    => "Autenticacao SAP",
        TipoAcaoEnum.NotaFiscalEntradaDraft   => "Nota Fiscal Entrada (Draft/Transito)",
        TipoAcaoEnum.NotaFiscalEntradaRecebimento => "Nota Fiscal Entrada (Recebimento)",
        TipoAcaoEnum.NotaFiscalSaida          => "Nota Fiscal Saida",
        TipoAcaoEnum.NotaFiscalTransito       => "Nota Fiscal de Transito",
        TipoAcaoEnum.DespesaImportacao        => "Despesa de Importacao",
        TipoAcaoEnum.LancamentoContabil       => "Lancamento Contabil",
        TipoAcaoEnum.Adiantamento             => "Adiantamento",
        TipoAcaoEnum.Pagamento                => "Pagamento",
        TipoAcaoEnum.ConsultaPedidoCompra     => "Consulta Pedido de Compra",
        TipoAcaoEnum.ConsultaPedidoVenda      => "Consulta Pedido de Venda",
        TipoAcaoEnum.ConsultaNotaFiscal       => "Consulta Nota Fiscal",
        TipoAcaoEnum.Cancelamento             => "Cancelamento",
        TipoAcaoEnum.Atualizacao              => "Atualizacao",
        TipoAcaoEnum.PedidoVenda              => "Pedido de Venda",
        TipoAcaoEnum.PedidoCompra             => "Pedido de Compra",
        TipoAcaoEnum.TransferenciaEstoque     => "Transferencia de Estoque",
        _                                     => "Desconhecido"
    };
}
