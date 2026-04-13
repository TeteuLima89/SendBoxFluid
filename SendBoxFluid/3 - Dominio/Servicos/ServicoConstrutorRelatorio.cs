using System.Text.Json;
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
        var dataAtual = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        return new RelatorioIntegracao
        {
            Chave = GerarChaveAleatoria(),
            TipoIntegracaoApi = "1",
            ResultadoIntegracao = sessao.Resultado == ResultadoIntegracaoEnum.Sucesso ? "Sucesso" : "Erro",
            DataEnvio = dataAtual,
            DataRetorno = dataAtual,
            Mensagem = MontarMensagem(sessao, nfeId),
            Entidade = "7",
            ChaveEntidade = GerarChaveAleatoria(),
            Json = requisicaoPrincipal?.CorpoRequisicao ?? "{}",
            Email = emailLog,
            Ativo = true
        };
    }

    private static RegistroRequisicao? ObterRequisicaoPrincipal(SessaoIntegracao sessao)
    {
        // A acao principal eh o ultimo POST que NAO eh Login nem GET
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

    private static string MontarMensagem(SessaoIntegracao sessao, string nfeId)
    {
        if (sessao.Resultado == ResultadoIntegracaoEnum.Sucesso)
        {
            return sessao.TipoAcao switch
            {
                TipoAcaoEnum.NotaFiscalEntradaDraft => $"Draft NF {nfeId} enviada com sucesso",
                TipoAcaoEnum.NotaFiscalEntradaRecebimento => $"Recebimento NF {nfeId} enviado com sucesso",
                TipoAcaoEnum.NotaFiscalTransito => $"NF {nfeId} enviada com sucesso (Transito)",
                TipoAcaoEnum.NotaFiscalSaida => $"NF Saida {nfeId} enviada com sucesso",
                TipoAcaoEnum.DespesaImportacao => $"Despesa importada com sucesso",
                TipoAcaoEnum.LancamentoContabil => $"Lancamento contabil registrado com sucesso",
                _ => "Integracao concluida com sucesso"
            };
        }

        return string.IsNullOrEmpty(sessao.Mensagem) ? "Erro na integracao" : sessao.Mensagem;
    }

    private static string GerarChaveAleatoria()
    {
        return Random.Shared.NextInt64(0, 100000000000000000L).ToString();
    }
}
