using SendBoxFluid.Dominio.Entidades;

namespace SendBoxFluid.Dominio.Interfaces.Repositorios;

public interface IRepositorioSessao
{
    SessaoIntegracao ObterOuCriar(string codigoSessao);
    SessaoIntegracao? ObterPorCodigo(string codigoSessao);
    List<SessaoIntegracao> ObterTodas();
    void Limpar();
}
