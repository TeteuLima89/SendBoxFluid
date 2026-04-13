using System.Net.Http.Headers;
using System.Text.Json;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Interfaces.Repositorios;

namespace SendBoxFluid.Infraestrutura.ClientesExternos;

/// <summary>
/// Cliente HTTP que conversa com a API do Narwal pra buscar dados originais
/// (NF via consulta-xml, processo, despesa, etc).
/// </summary>
public class ClienteNarwal
{
    private static readonly HttpClient _clienteHttp = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly IRepositorioConfiguracaoNarwal _repositorioConfiguracao;
    private readonly ILogger<ClienteNarwal> _registradorEventos;

    public ClienteNarwal(
        IRepositorioConfiguracaoNarwal repositorioConfiguracao,
        ILogger<ClienteNarwal> registradorEventos)
    {
        _repositorioConfiguracao = repositorioConfiguracao;
        _registradorEventos = registradorEventos;
    }

    /// <summary>
    /// Busca a NF completa via consulta-xml (mesmo endpoint que o fluxo Fluid usa).
    /// </summary>
    public async Task<string?> BuscarNotaFiscal(string nfeId, string? cliente = null)
        => await BuscarComToken(cliente, async token =>
        {
            var configuracao = ObterConfiguracao(cliente);
            if (configuracao == null) return null;
            var url = $"{configuracao.UrlNarwal.TrimEnd('/')}/api/nfe/v3/retornaxml?id={nfeId}";
            return await ExecutarGet(url, token);
        });

    /// <summary>
    /// Busca processo de importacao via OData.
    /// </summary>
    public async Task<string?> BuscarProcesso(string processoId, string? cliente = null)
        => await BuscarComToken(cliente, async token =>
        {
            var configuracao = ObterConfiguracao(cliente);
            if (configuracao == null) return null;
            var url = $"{configuracao.UrlNarwal.TrimEnd('/')}/odata/ODataProcesso?$filter=ProcessoId%20eq%20{processoId}";
            return await ExecutarGet(url, token);
        });

    /// <summary>
    /// Busca despesa via OData.
    /// </summary>
    public async Task<string?> BuscarDespesa(string despesaId, string? cliente = null)
        => await BuscarComToken(cliente, async token =>
        {
            var configuracao = ObterConfiguracao(cliente);
            if (configuracao == null) return null;
            var url = $"{configuracao.UrlNarwal.TrimEnd('/')}/odata/ODataDespesa?$filter=DespesaId%20eq%20{despesaId}";
            return await ExecutarGet(url, token);
        });

    private async Task<string?> BuscarComToken(string? cliente, Func<string, Task<string?>> acao)
    {
        var configuracao = ObterConfiguracao(cliente);
        if (configuracao == null)
        {
            _registradorEventos.LogDebug("Sem configuracao Narwal - dados originais nao serao enriquecidos");
            return null;
        }

        var token = await ObterToken(configuracao);
        if (string.IsNullOrEmpty(token)) return null;

        try
        {
            return await acao(token);
        }
        catch (Exception ex)
        {
            _registradorEventos.LogWarning("Falha ao buscar dados Narwal: {Mensagem}", ex.Message);
            return null;
        }
    }

    private ConfiguracaoNarwal? ObterConfiguracao(string? cliente)
    {
        if (!string.IsNullOrEmpty(cliente))
        {
            var porCliente = _repositorioConfiguracao.ObterPorCliente(cliente);
            if (porCliente != null) return porCliente;
        }
        return _repositorioConfiguracao.ObterPadrao();
    }

    private async Task<string?> ObterToken(ConfiguracaoNarwal configuracao)
    {
        // Reutiliza token se ainda valido
        if (!string.IsNullOrEmpty(configuracao.TokenAtual) &&
            configuracao.TokenExpiraEm.HasValue &&
            configuracao.TokenExpiraEm.Value > DateTime.UtcNow.AddMinutes(1))
        {
            return configuracao.TokenAtual;
        }

        try
        {
            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = configuracao.Usuario,
                ["password"] = configuracao.Senha
            });

            var url = $"{configuracao.UrlNarwal.TrimEnd('/')}/api/security/token";
            var resposta = await _clienteHttp.PostAsync(url, formData);
            if (!resposta.IsSuccessStatusCode)
            {
                _registradorEventos.LogWarning("Auth Narwal falhou: HTTP {Status}", (int)resposta.StatusCode);
                return null;
            }

            var corpo = await resposta.Content.ReadAsStringAsync();
            using var documento = JsonDocument.Parse(corpo);
            var token = documento.RootElement.GetProperty("access_token").GetString();
            var expiraEm = documento.RootElement.TryGetProperty("expires_in", out var exp)
                ? exp.GetInt32() : 3600;

            configuracao.TokenAtual = token;
            configuracao.TokenExpiraEm = DateTime.UtcNow.AddSeconds(expiraEm);
            return token;
        }
        catch (Exception ex)
        {
            _registradorEventos.LogWarning("Falha ao autenticar Narwal: {Mensagem}", ex.Message);
            return null;
        }
    }

    private async Task<string?> ExecutarGet(string url, string token)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, url);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resposta = await _clienteHttp.SendAsync(requisicao);
        if (!resposta.IsSuccessStatusCode) return null;
        return await resposta.Content.ReadAsStringAsync();
    }
}
