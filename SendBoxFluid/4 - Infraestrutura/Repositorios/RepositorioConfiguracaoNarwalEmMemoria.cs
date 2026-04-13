using System.Collections.Concurrent;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Interfaces.Repositorios;

namespace SendBoxFluid.Infraestrutura.Repositorios;

public class RepositorioConfiguracaoNarwalEmMemoria : IRepositorioConfiguracaoNarwal
{
    private readonly ConcurrentDictionary<string, ConfiguracaoNarwal> _configuracoes = new();

    public void Salvar(ConfiguracaoNarwal configuracao)
        => _configuracoes[configuracao.Cliente.ToLower()] = configuracao;

    public ConfiguracaoNarwal? ObterPorCliente(string cliente)
        => _configuracoes.TryGetValue(cliente.ToLower(), out var cfg) ? cfg : null;

    public ConfiguracaoNarwal? ObterPadrao()
        => _configuracoes.Values.FirstOrDefault();

    public List<ConfiguracaoNarwal> ListarTodas()
        => _configuracoes.Values.ToList();

    public void Remover(string cliente)
        => _configuracoes.TryRemove(cliente.ToLower(), out _);
}
