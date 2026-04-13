using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SendBoxFluid.Dominio.Interfaces.Repositorios;
using SendBoxFluid.Infraestrutura.Persistencia;

namespace SendBoxFluid.Infraestrutura.Repositorios;

public class RepositorioDocumentoPostgres : IRepositorioDocumento
{
    private readonly IServiceScopeFactory _fabricaEscopo;
    private static readonly object _bloqueioContador = new();

    public RepositorioDocumentoPostgres(IServiceScopeFactory fabricaEscopo)
    {
        _fabricaEscopo = fabricaEscopo;
    }

    public int ProximoCodigoEntrada() => IncrementarContador("DocEntry", inicio: 1000);
    public int ProximoNumeroLancamento() => IncrementarContador("JdtNum", inicio: 5000);

    public void Adicionar(string entidade, JsonObject documento)
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();
        contexto.Documentos.Add(new DocumentoArmazenado
        {
            Entidade = entidade,
            DadosJson = documento.ToJsonString()
        });
        contexto.SaveChanges();
    }

    public List<JsonObject> Consultar(string entidade)
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();
        return contexto.Documentos
            .Where(d => d.Entidade == entidade)
            .Select(d => d.DadosJson)
            .AsEnumerable()
            .Select(json => JsonNode.Parse(json)?.AsObject() ?? new JsonObject())
            .ToList();
    }

    public JsonObject? BuscarPorCodigoEntrada(string entidade, int codigoEntrada)
    {
        return Consultar(entidade).FirstOrDefault(d =>
            d.TryGetPropertyValue("DocEntry", out var v) && v?.GetValue<int>() == codigoEntrada);
    }

    public void Limpar()
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();
        contexto.Documentos.ExecuteDelete();
        contexto.Contadores.ExecuteDelete();
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<JsonObject>> ObterTodos()
    {
        using var escopo = _fabricaEscopo.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();
        var todos = contexto.Documentos.ToList();
        var resultado = new Dictionary<string, IReadOnlyCollection<JsonObject>>();
        foreach (var grupo in todos.GroupBy(d => d.Entidade))
        {
            resultado[grupo.Key] = grupo
                .Select(d => JsonNode.Parse(d.DadosJson)?.AsObject() ?? new JsonObject())
                .ToList()
                .AsReadOnly();
        }
        return resultado;
    }

    private int IncrementarContador(string nome, int inicio)
    {
        lock (_bloqueioContador)
        {
            using var escopo = _fabricaEscopo.CreateScope();
            var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();

            var contador = contexto.Contadores.FirstOrDefault(c => c.Nome == nome);
            if (contador == null)
            {
                contador = new ContadorEntidade { Nome = nome, Valor = inicio };
                contexto.Contadores.Add(contador);
            }
            contador.Valor++;
            contexto.SaveChanges();
            return contador.Valor;
        }
    }
}
