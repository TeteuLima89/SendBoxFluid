namespace SendBoxFluid.Apresentacao.ViewModels;

public class SessaoDetalheViewModel
{
    public string CodigoSessao { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataUltimaAtividade { get; set; }
    public string TipoAcao { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? IdentificadorNegocio { get; set; }
    public List<RequisicaoViewModel> Requisicoes { get; set; } = new();

    // Campos do RelatorioIntegracao (formato Narwal)
    public string JsonRelatorio { get; set; } = string.Empty;
    public string ResultadoRelatorio { get; set; } = string.Empty;
    public string MensagemRelatorio { get; set; } = string.Empty;
    public string DataEnvioRelatorio { get; set; } = string.Empty;
}

public class RequisicaoViewModel
{
    public Guid Identificador { get; set; }
    public DateTime DataHora { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public string Caminho { get; set; } = string.Empty;
    public string? Entidade { get; set; }
    public int CodigoStatusHttp { get; set; }
    public string CorpoRequisicao { get; set; } = string.Empty;
    public string CorpoResposta { get; set; } = string.Empty;
}
