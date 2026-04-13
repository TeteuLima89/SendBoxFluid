using SendBoxFluid.Dominio.Servicos;

namespace SendBoxFluid.Dominio.Entidades;

/// <summary>
/// Registro de uma requisicao HTTP recebida pelo SendBox.
/// Cada chamada do fluxo Fluid (Login, GET, POST, PATCH) gera um registro.
/// </summary>
public class RegistroRequisicao
{
    public Guid Identificador { get; private set; }
    public DateTime DataHora { get; private set; }
    public string Metodo { get; private set; } = string.Empty;
    public string Caminho { get; private set; } = string.Empty;
    public string? CodigoSessao { get; private set; }
    public string CorpoRequisicao { get; private set; } = string.Empty;
    public string CorpoResposta { get; private set; } = string.Empty;
    public int CodigoStatusHttp { get; private set; }
    public string? Entidade { get; private set; }

    // Construtor privado para o EF Core
    private RegistroRequisicao() { }

    public RegistroRequisicao(
        string metodo,
        string caminho,
        string? codigoSessao,
        string corpoRequisicao,
        string corpoResposta,
        int codigoStatusHttp,
        string? entidade)
    {
        Identificador = Guid.NewGuid();
        DataHora = ServicoFusoHorario.AgoraBrasilia();
        Metodo = metodo;
        Caminho = caminho;
        CodigoSessao = codigoSessao;
        CorpoRequisicao = corpoRequisicao;
        CorpoResposta = corpoResposta;
        CodigoStatusHttp = codigoStatusHttp;
        Entidade = entidade;
    }
}
