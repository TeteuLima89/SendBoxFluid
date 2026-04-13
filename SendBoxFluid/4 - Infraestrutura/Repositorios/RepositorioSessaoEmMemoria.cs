using System.Collections.Concurrent;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Interfaces.Repositorios;

namespace SendBoxFluid.Infraestrutura.Repositorios;

/// <summary>
/// Repositorio em memoria que guarda as sessoes de integracao
/// (cada execucao do fluxo Fluid identificada pelo B1SESSION).
/// </summary>
public class RepositorioSessaoEmMemoria : IRepositorioSessao
{
    private readonly ConcurrentDictionary<string, SessaoIntegracao> _sessoes = new();

    public SessaoIntegracao ObterOuCriar(string codigoSessao)
    {
        return _sessoes.GetOrAdd(codigoSessao, codigo => new SessaoIntegracao(codigo));
    }

    public SessaoIntegracao? ObterPorCodigo(string codigoSessao)
    {
        return _sessoes.TryGetValue(codigoSessao, out var sessao) ? sessao : null;
    }

    public List<SessaoIntegracao> ObterTodas()
    {
        return _sessoes.Values.OrderByDescending(s => s.DataInicio).ToList();
    }

    public void Limpar() => _sessoes.Clear();
}
