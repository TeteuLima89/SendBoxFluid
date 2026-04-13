using SendBoxFluid.Dominio.Servicos;

namespace SendBoxFluid.Dominio.Entidades;

/// <summary>
/// Registro de uma requisicao HTTP recebida pelo SendBox.
/// Cada chamada do fluxo Fluid (Login, GET, POST, PATCH) gera um registro.
/// </summary>
public class RegistroRequisicao
{
    public Guid Identificador { get; }
    public DateTime DataHora { get; }
    public string Metodo { get; }
    public string Caminho { get; }
    public string? CodigoSessao { get; }
    public string CorpoRequisicao { get; }
    public string CorpoResposta { get; }
    public int CodigoStatusHttp { get; }
    public string? Entidade { get; }

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
