using SendBoxFluid.Dominio.Entidades;

namespace SendBoxFluid.Dominio.Interfaces.Repositorios;

public interface IRepositorioConfiguracaoNarwal
{
    void Salvar(ConfiguracaoNarwal configuracao);
    ConfiguracaoNarwal? ObterPorCliente(string cliente);
    ConfiguracaoNarwal? ObterPadrao();
    List<ConfiguracaoNarwal> ListarTodas();
    void Remover(string cliente);
}
