namespace SendBoxFluid.Dominio.Entidades;

/// <summary>
/// Espelha o JSON que o passo "retornointegracao" do Fluid envia pro Narwal.
/// Usado pra mostrar ao QA o que seria reportado e permitir download.
/// </summary>
public class RelatorioIntegracao
{
    public string Chave { get; set; } = string.Empty;
    public string TipoIntegracaoApi { get; set; } = "1";
    public string ResultadoIntegracao { get; set; } = string.Empty;
    public string DataEnvio { get; set; } = string.Empty;
    public string DataRetorno { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string Entidade { get; set; } = "7";
    public string ChaveEntidade { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
