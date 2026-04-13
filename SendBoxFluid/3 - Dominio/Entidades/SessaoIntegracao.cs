using SendBoxFluid.Dominio.Enumeradores;
using SendBoxFluid.Dominio.Servicos;

namespace SendBoxFluid.Dominio.Entidades;

/// <summary>
/// Cada execucao de "POST principal" do fluxo Fluid (1 NF, 1 despesa, etc).
/// Identificada por GUID gerado no momento que o POST chega.
/// </summary>
public class SessaoIntegracao
{
    public string CodigoSessao { get; private set; } = string.Empty;
    public DateTime DataInicio { get; private set; }
    public DateTime DataUltimaAtividade { get; private set; }
    public List<RegistroRequisicao> Requisicoes { get; private set; } = new();
    public TipoAcaoEnum TipoAcao { get; set; }
    public TipoErpEnum TipoErp { get; set; }
    public ResultadoIntegracaoEnum Resultado { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string? PayloadEnviadoErp { get; set; }
    public string? RespostaErp { get; set; }
    public string? IdentificadorNegocio { get; set; }

    // Construtor privado para o EF Core
    private SessaoIntegracao() { }

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
