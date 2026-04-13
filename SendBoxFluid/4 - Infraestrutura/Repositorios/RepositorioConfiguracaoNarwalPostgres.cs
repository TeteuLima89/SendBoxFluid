using Microsoft.EntityFrameworkCore;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Interfaces.Repositorios;
using SendBoxFluid.Infraestrutura.Persistencia;

namespace SendBoxFluid.Infraestrutura.Repositorios;

public class RepositorioConfiguracaoNarwalPostgres : IRepositorioConfiguracaoNarwal
{
    private readonly IServiceScopeFactory _fabricaEscopo;

    public RepositorioConfiguracaoNarwalPostgres(IServiceScopeFactory fabricaEscopo)
    {
        _fabricaEscopo = fabricaEscopo;
    }

    public void Salvar(ConfiguracaoNarwal configuracao)
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();
        var existente = contexto.ConfiguracoesNarwal.Find(configuracao.Cliente);
        if (existente == null)
        {
            contexto.ConfiguracoesNarwal.Add(configuracao);
        }
        else
        {
            existente.UrlNarwal = configuracao.UrlNarwal;
            existente.Usuario = configuracao.Usuario;
            existente.Senha = configuracao.Senha;
        }
        contexto.SaveChanges();
    }

    public ConfiguracaoNarwal? ObterPorCliente(string cliente)
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();
        return contexto.ConfiguracoesNarwal.Find(cliente);
    }

    public ConfiguracaoNarwal? ObterPadrao()
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();
        return contexto.ConfiguracoesNarwal.FirstOrDefault();
    }

    public List<ConfiguracaoNarwal> ListarTodas()
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();
        return contexto.ConfiguracoesNarwal.ToList();
    }

    public void Remover(string cliente)
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();
        var existente = contexto.ConfiguracoesNarwal.Find(cliente);
        if (existente != null)
        {
            contexto.ConfiguracoesNarwal.Remove(existente);
            contexto.SaveChanges();
        }
    }
}
