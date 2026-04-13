using Microsoft.EntityFrameworkCore;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Interfaces.Repositorios;
using SendBoxFluid.Infraestrutura.Persistencia;

namespace SendBoxFluid.Infraestrutura.Repositorios;

/// <summary>
/// Repositorio de sessoes persistido em PostgreSQL.
/// As sessoes ficam guardadas indefinidamente no banco.
/// </summary>
public class RepositorioSessaoPostgres : IRepositorioSessao
{
    private readonly IServiceScopeFactory _fabricaEscopo;

    public RepositorioSessaoPostgres(IServiceScopeFactory fabricaEscopo)
    {
        _fabricaEscopo = fabricaEscopo;
    }

    public SessaoIntegracao ObterOuCriar(string codigoSessao)
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();

        var existente = contexto.Sessoes
            .Include(s => s.Requisicoes)
            .FirstOrDefault(s => s.CodigoSessao == codigoSessao);

        if (existente != null) return existente;

        var nova = new SessaoIntegracao(codigoSessao);
        contexto.Sessoes.Add(nova);
        contexto.SaveChanges();
        return nova;
    }

    public SessaoIntegracao? ObterPorCodigo(string codigoSessao)
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();

        return contexto.Sessoes
            .Include(s => s.Requisicoes)
            .FirstOrDefault(s => s.CodigoSessao == codigoSessao);
    }

    public List<SessaoIntegracao> ObterTodas()
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();

        return contexto.Sessoes
            .Include(s => s.Requisicoes)
            .OrderByDescending(s => s.DataInicio)
            .Take(500) // Limite pra nao explodir o painel
            .ToList();
    }

    public void Limpar()
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();

        contexto.Requisicoes.ExecuteDelete();
        contexto.Sessoes.ExecuteDelete();
    }

    /// <summary>
    /// Salva mudancas pendentes em uma sessao (chamado pelo middleware
    /// quando atualiza propriedades depois de criada).
    /// </summary>
    public void Salvar(SessaoIntegracao sessao)
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();

        var existente = contexto.Sessoes
            .Include(s => s.Requisicoes)
            .FirstOrDefault(s => s.CodigoSessao == sessao.CodigoSessao);

        if (existente == null)
        {
            contexto.Sessoes.Add(sessao);
        }
        else
        {
            // Atualiza propriedades editaveis
            contexto.Entry(existente).CurrentValues.SetValues(sessao);
            // Adiciona novas requisicoes
            foreach (var req in sessao.Requisicoes)
            {
                if (!existente.Requisicoes.Any(r => r.Identificador == req.Identificador))
                {
                    existente.Requisicoes.Add(req);
                }
            }
        }
        contexto.SaveChanges();
    }
}
