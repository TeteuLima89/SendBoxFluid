using SendBoxFluid.Dominio.Enumeradores;
using SendBoxFluid.Dominio.Servicos;

namespace SendBoxFluid.Dominio.Entidades;

/// <summary>
/// Agrupa todas as requisicoes de UMA execucao de fluxo Fluid.
/// Identificada pelo cookie B1SESSION retornado no Login.
/// </summary>
public class SessaoIntegracao
{
    public string CodigoSessao { get; }
    public DateTime DataInicio { get; }
    public DateTime DataUltimaAtividade { get; private set; }
    public List<RegistroRequisicao> Requisicoes { get; }
    public TipoAcaoEnum TipoAcao { get; set; }
    public TipoErpEnum TipoErp { get; set; }
    public ResultadoIntegracaoEnum Resultado { get; set; }
    public string Mensagem { get; set; }
    public string? PayloadEnviadoErp { get; set; }
    public string? RespostaErp { get; set; }
    public string? IdentificadorNegocio { get; set; }
    public string? DadosOriginaisNarwal { get; set; }

    public SessaoIntegracao(string codigoSessao)
    {
        CodigoSessao = codigoSessao;
        DataInicio = ServicoFusoHorario.AgoraBrasilia();
        DataUltimaAtividade = DataInicio;
        Requisicoes = new List<RegistroRequisicao>();
        TipoAcao = TipoAcaoEnum.Login;
        TipoErp = TipoErpEnum.Desconhecido;
        Resultado = ResultadoIntegracaoEnum.EmAndamento;
        Mensagem = string.Empty;
    }

    public void AdicionarRequisicao(RegistroRequisicao requisicao)
    {
        Requisicoes.Add(requisicao);
        DataUltimaAtividade = ServicoFusoHorario.AgoraBrasilia();
    }
}
