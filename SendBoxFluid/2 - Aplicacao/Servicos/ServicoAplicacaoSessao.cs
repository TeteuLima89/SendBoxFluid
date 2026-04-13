using SendBoxFluid.Aplicacao.Interfaces;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Interfaces.Repositorios;
using SendBoxFluid.Dominio.Servicos;

namespace SendBoxFluid.Aplicacao.Servicos;

public class ServicoAplicacaoSessao : IServicoAplicacaoSessao
{
    private readonly IRepositorioSessao _repositorioSessao;

    public ServicoAplicacaoSessao(IRepositorioSessao repositorioSessao)
    {
        _repositorioSessao = repositorioSessao;
    }

    public List<SessaoIntegracao> ListarTodas() => _repositorioSessao.ObterTodas();

    public SessaoIntegracao? ObterPorCodigo(string codigoSessao)
        => _repositorioSessao.ObterPorCodigo(codigoSessao);

    public RelatorioIntegracao? ConstruirRelatorio(string codigoSessao)
    {
        var sessao = _repositorioSessao.ObterPorCodigo(codigoSessao);
        return sessao == null ? null : ServicoConstrutorRelatorio.Construir(sessao);
    }

    public void Limpar() => _repositorioSessao.Limpar();
}
