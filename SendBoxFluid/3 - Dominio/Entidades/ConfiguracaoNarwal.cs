namespace SendBoxFluid.Dominio.Entidades;

/// <summary>
/// Configuracao de acesso a uma instancia do Narwal (por cliente).
/// O QA configura uma vez via /sandbox/configurar-narwal e o SendBox
/// usa pra enriquecer cada sessao com os dados originais (consulta-xml,
/// consulta-processo, etc).
/// </summary>
public class ConfiguracaoNarwal
{
    public string Cliente { get; set; } = string.Empty;
    public string UrlNarwal { get; set; } = string.Empty;
    public string Usuario { get; set; } = "integracao";
    public string Senha { get; set; } = string.Empty;
    public string? TokenAtual { get; set; }
    public DateTime? TokenExpiraEm { get; set; }
}
