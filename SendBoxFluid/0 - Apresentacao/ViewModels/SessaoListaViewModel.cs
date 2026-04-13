namespace SendBoxFluid.Apresentacao.ViewModels;

public class SessaoListaViewModel
{
    public string CodigoSessao { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataUltimaAtividade { get; set; }
    public string TipoAcao { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public int QuantidadeRequisicoes { get; set; }
}
