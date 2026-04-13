using SendBoxFluid.Dominio.Entidades;

namespace SendBoxFluid.Aplicacao.Interfaces;

public interface IServicoAplicacaoSessao
{
    List<SessaoIntegracao> ListarTodas();
    SessaoIntegracao? ObterPorCodigo(string codigoSessao);
    RelatorioIntegracao? ConstruirRelatorio(string codigoSessao);
    void Limpar();
}
