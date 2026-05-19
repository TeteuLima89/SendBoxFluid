using System.Text.Json.Nodes;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Enumeradores;

namespace SendBoxFluid.Dominio.Servicos;

/// <summary>
/// Constroi o RelatorioIntegracao no MESMO formato que o passo
/// "retornointegracao" do Fluid envia pro Narwal.
/// Permite ao QA visualizar e baixar o JSON de sucesso/erro.
/// </summary>
public static class ServicoConstrutorRelatorio
{
    public static RelatorioIntegracao Construir(SessaoIntegracao sessao, string emailLog = "fluid@narwalsistemas.com.br")
    {
        var requisicaoPrincipal = ObterRequisicaoPrincipal(sessao);
        var nfeId = ExtrairNfeId(requisicaoPrincipal);
        var (tipoIntegracao, codigoEntidade) = ObterMapeamentoTipo(sessao.TipoAcao);

        return new RelatorioIntegracao
        {
            Chave = sessao.IdentificadorNegocio ?? GerarChaveAleatoria(),
            TipoIntegracaoApi = tipoIntegracao,
            ResultadoIntegracao = sessao.Resultado == ResultadoIntegracaoEnum.Sucesso ? "Sucesso" : "Erro",
            DataEnvio = sessao.DataInicio.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            DataRetorno = sessao.DataUltimaAtividade.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Mensagem = MontarMensagem(sessao, nfeId),
            Entidade = codigoEntidade,
            ChaveEntidade = ExtrairChaveEntidade(sessao),
            Json = requisicaoPrincipal?.CorpoRequisicao ?? "{}",
            Email = emailLog,
            Ativo = true
        };
    }

    private static RegistroRequisicao? ObterRequisicaoPrincipal(SessaoIntegracao sessao)
    {
        return sessao.Requisicoes
            .Where(r => r.Metodo.Equals("POST", StringComparison.OrdinalIgnoreCase))
            .Where(r => !r.Caminho.Contains("/Login", StringComparison.OrdinalIgnoreCase))
            .LastOrDefault();
    }

    private static string ExtrairNfeId(RegistroRequisicao? requisicao)
    {
        if (requisicao == null) return "?";
        try
        {
            var json = JsonNode.Parse(requisicao.CorpoRequisicao)?.AsObject();
            return json?["U_ACT_NfeId"]?.ToString() ?? "?";
        }
        catch
        {
            return "?";
        }
    }

    private static string ExtrairChaveEntidade(SessaoIntegracao sessao)
    {
        try
        {
            var json = JsonNode.Parse(sessao.RespostaErp ?? "")?.AsObject();
            if (json != null)
            {
                if (json["DocEntry"] is JsonNode docEntry) return docEntry.ToString();
                if (json["JdtNum"] is JsonNode jdtNum) return jdtNum.ToString();
            }
        }
        catch { }
        return GerarChaveAleatoria();
    }

    private static (string TipoIntegracao, string CodigoEntidade) ObterMapeamentoTipo(TipoAcaoEnum acao) => acao switch
    {
        TipoAcaoEnum.NotaFiscalEntradaDraft       => ("2",  "8"),
        TipoAcaoEnum.NotaFiscalEntradaRecebimento => ("2",  "8"),
        TipoAcaoEnum.NotaFiscalTransito           => ("14", "8"),
        TipoAcaoEnum.NotaFiscalSaida              => ("1",  "7"),
        TipoAcaoEnum.DespesaImportacao            => ("3",  "9"),
        TipoAcaoEnum.LancamentoContabil           => ("4",  "10"),
        TipoAcaoEnum.Adiantamento                 => ("5",  "11"),
        TipoAcaoEnum.Pagamento                    => ("9",  "15"),
        TipoAcaoEnum.PedidoVenda                  => ("6",  "12"),
        TipoAcaoEnum.PedidoCompra                 => ("7",  "13"),
        TipoAcaoEnum.TransferenciaEstoque         => ("14", "8"),
        _                                         => ("1",  "7"),
    };

    private static string MontarMensagem(SessaoIntegracao sessao, string nfeId)
    {
        if (sessao.Resultado == ResultadoIntegracaoEnum.Sucesso)
        {
            return sessao.TipoAcao switch
            {
                TipoAcaoEnum.NotaFiscalEntradaDraft       => $"Draft NF {nfeId} enviada com sucesso",
                TipoAcaoEnum.NotaFiscalEntradaRecebimento => $"Recebimento NF {nfeId} enviado com sucesso",
                TipoAcaoEnum.NotaFiscalTransito           => $"NF {nfeId} enviada com sucesso (Transito)",
                TipoAcaoEnum.NotaFiscalSaida              => $"NF Saida {nfeId} enviada com sucesso",
                TipoAcaoEnum.DespesaImportacao            => "Despesa importada com sucesso",
                TipoAcaoEnum.LancamentoContabil           => "Lancamento contabil registrado com sucesso",
                TipoAcaoEnum.PedidoVenda                  => "Pedido de venda integrado com sucesso",
                TipoAcaoEnum.PedidoCompra                 => "Pedido de compra integrado com sucesso",
                TipoAcaoEnum.TransferenciaEstoque         => "Transferencia de estoque registrada com sucesso",
                _                                         => "Integracao concluida com sucesso"
            };
        }

        return string.IsNullOrEmpty(sessao.Mensagem) ? "Erro na integracao" : sessao.Mensagem;
    }

    private static string GerarChaveAleatoria()
    {
        return Random.Shared.NextInt64(0, 100000000000000000L).ToString();
    }
}
