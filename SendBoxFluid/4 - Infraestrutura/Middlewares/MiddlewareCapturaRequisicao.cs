using System.Collections.Concurrent;
using System.Text;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Enumeradores;
using SendBoxFluid.Dominio.Interfaces.Repositorios;
using SendBoxFluid.Dominio.Servicos;
using SendBoxFluid.Infraestrutura.ClientesExternos;

namespace SendBoxFluid.Infraestrutura.Middlewares;

/// <summary>
/// Intercepta toda requisicao HTTP, captura corpo e resposta.
/// Cria nova SessaoIntegracao a cada POST principal (Draft, LandedCost, etc).
/// GETs e Login que vieram antes sao associados a essa sessao.
/// Cada NF/transito/reintegracao = item proprio no painel.
/// </summary>
public class MiddlewareCapturaRequisicao
{
    /// <summary>
    /// Buffer por cookie B1SESSION. Acumula Login + GETs ate vir o POST principal,
    /// que finaliza uma SessaoIntegracao com tudo agrupado.
    /// </summary>
    private static readonly ConcurrentDictionary<string, List<RegistroRequisicao>> _bufferPorCookie = new();

    private readonly RequestDelegate _proximo;

    public MiddlewareCapturaRequisicao(RequestDelegate proximo)
    {
        _proximo = proximo;
    }

    public async Task InvokeAsync(HttpContext contexto, IRepositorioSessao repositorioSessao, ClienteNarwal clienteNarwal)
    {
        var caminho = contexto.Request.Path.Value ?? "";
        if (!caminho.StartsWith("/b1s", StringComparison.OrdinalIgnoreCase) &&
            !caminho.StartsWith("/mge", StringComparison.OrdinalIgnoreCase) &&
            !caminho.StartsWith("/sankhya", StringComparison.OrdinalIgnoreCase))
        {
            await _proximo(contexto);
            return;
        }

        var corpoRequisicao = await LerCorpoRequisicao(contexto);
        var streamOriginalResposta = contexto.Response.Body;
        using var streamMemoriaResposta = new MemoryStream();
        contexto.Response.Body = streamMemoriaResposta;

        try
        {
            await _proximo(contexto);
        }
        finally
        {
            var corpoResposta = await LerCorpoResposta(streamMemoriaResposta);
            await streamMemoriaResposta.CopyToAsync(streamOriginalResposta);
            contexto.Response.Body = streamOriginalResposta;

            ProcessarRequisicao(contexto, corpoRequisicao, corpoResposta, repositorioSessao, clienteNarwal);
        }
    }

    private static async Task<string> LerCorpoRequisicao(HttpContext contexto)
    {
        contexto.Request.EnableBuffering();
        using var leitor = new StreamReader(contexto.Request.Body, Encoding.UTF8, leaveOpen: true);
        var corpo = await leitor.ReadToEndAsync();
        contexto.Request.Body.Position = 0;
        return corpo;
    }

    private static async Task<string> LerCorpoResposta(MemoryStream stream)
    {
        stream.Position = 0;
        using var leitor = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var corpo = await leitor.ReadToEndAsync();
        stream.Position = 0;
        return corpo;
    }

    private static void ProcessarRequisicao(
        HttpContext contexto,
        string corpoRequisicao,
        string corpoResposta,
        IRepositorioSessao repositorioSessao,
        ClienteNarwal clienteNarwal)
    {
        var codigoCookie = ExtrairCodigoCookie(contexto, corpoResposta);
        if (string.IsNullOrEmpty(codigoCookie))
            return;

        var entidade = ExtrairEntidade(contexto.Request.Path);
        var registro = new RegistroRequisicao(
            metodo: contexto.Request.Method,
            caminho: contexto.Request.Path + contexto.Request.QueryString,
            codigoSessao: codigoCookie,
            corpoRequisicao: corpoRequisicao,
            corpoResposta: corpoResposta,
            codigoStatusHttp: contexto.Response.StatusCode,
            entidade: entidade);

        var ehPostPrincipal = EhPostPrincipal(registro);

        if (ehPostPrincipal)
        {
            // Cria NOVA sessao (cada NF/Transito/Reintegracao eh independente)
            FinalizarComoNovaSessao(codigoCookie, registro, contexto.Request.Path, repositorioSessao, clienteNarwal);
        }
        else
        {
            // Login ou GET - bufferiza pra associar ao proximo POST principal
            BufferizarRequisicao(codigoCookie, registro);
        }
    }

    private static bool EhPostPrincipal(RegistroRequisicao registro)
    {
        if (!registro.Metodo.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
            !registro.Metodo.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
            return false;

        // Login nao eh POST principal
        if (registro.Caminho.Contains("/Login", StringComparison.OrdinalIgnoreCase))
            return false;

        // Sankhya: Login via serviceName
        if (registro.Caminho.Contains("MobileLoginSP.login", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static void BufferizarRequisicao(string codigoCookie, RegistroRequisicao registro)
    {
        var buffer = _bufferPorCookie.GetOrAdd(codigoCookie, _ => new List<RegistroRequisicao>());
        lock (buffer)
        {
            buffer.Add(registro);
            // Limita buffer pra evitar memory leak
            if (buffer.Count > 50)
                buffer.RemoveAt(0);
        }
    }

    private static void FinalizarComoNovaSessao(
        string codigoCookie,
        RegistroRequisicao postPrincipal,
        PathString caminho,
        IRepositorioSessao repositorioSessao,
        ClienteNarwal clienteNarwal)
    {
        // Codigo unico pra essa integracao (nao mais o cookie compartilhado)
        var codigoIntegracao = Guid.NewGuid().ToString();
        var sessao = repositorioSessao.ObterOuCriar(codigoIntegracao);

        // Pega buffer atual e move pra essa sessao
        if (_bufferPorCookie.TryGetValue(codigoCookie, out var buffer))
        {
            lock (buffer)
            {
                foreach (var requisicaoBuferizada in buffer)
                    sessao.AdicionarRequisicao(requisicaoBuferizada);
                buffer.Clear();
            }
        }

        // Adiciona o POST principal
        sessao.AdicionarRequisicao(postPrincipal);
        sessao.TipoErp = IdentificarErp(caminho);

        // Identifica tipo de acao e resultado
        var tipoAcao = ServicoIdentificadorAcao.Identificar(
            postPrincipal.Metodo, postPrincipal.Caminho, postPrincipal.CorpoRequisicao);
        sessao.TipoAcao = tipoAcao;

        if (postPrincipal.CodigoStatusHttp >= 400)
        {
            sessao.Resultado = ResultadoIntegracaoEnum.Erro;
            sessao.Mensagem = $"HTTP {postPrincipal.CodigoStatusHttp} em {postPrincipal.Caminho}";
        }
        else
        {
            sessao.Resultado = ResultadoIntegracaoEnum.Sucesso;
            sessao.PayloadEnviadoErp = postPrincipal.CorpoRequisicao;
            sessao.RespostaErp = postPrincipal.CorpoResposta;

            var identificador = ServicoExtratorIdentificador.Extrair(postPrincipal.CorpoRequisicao);
            if (!string.IsNullOrEmpty(identificador))
                sessao.IdentificadorNegocio = identificador;
        }

        // Enriquece com dados do Narwal de forma assincrona
        if (!string.IsNullOrEmpty(sessao.IdentificadorNegocio))
        {
            _ = EnriquecerComDadosNarwal(sessao, clienteNarwal);
        }
    }

    private static async Task EnriquecerComDadosNarwal(SessaoIntegracao sessao, ClienteNarwal clienteNarwal)
    {
        try
        {
            string? dados = sessao.TipoAcao switch
            {
                TipoAcaoEnum.NotaFiscalEntradaDraft or
                TipoAcaoEnum.NotaFiscalEntradaRecebimento or
                TipoAcaoEnum.NotaFiscalTransito or
                TipoAcaoEnum.NotaFiscalSaida =>
                    await clienteNarwal.BuscarNotaFiscal(sessao.IdentificadorNegocio!),
                TipoAcaoEnum.DespesaImportacao =>
                    await clienteNarwal.BuscarDespesa(sessao.IdentificadorNegocio!),
                _ => await clienteNarwal.BuscarProcesso(sessao.IdentificadorNegocio!)
            };

            if (!string.IsNullOrEmpty(dados))
                sessao.DadosOriginaisNarwal = dados;
        }
        catch { }
    }

    private static TipoErpEnum IdentificarErp(PathString caminho)
    {
        var path = caminho.Value ?? "";
        if (path.StartsWith("/b1s", StringComparison.OrdinalIgnoreCase))
            return TipoErpEnum.SapB1;
        if (path.StartsWith("/mge", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/sankhya", StringComparison.OrdinalIgnoreCase))
            return TipoErpEnum.Sankhya;
        return TipoErpEnum.Desconhecido;
    }

    private static string? ExtrairCodigoCookie(HttpContext contexto, string corpoResposta)
    {
        // No Login, o codigo de sessao vem no corpo da resposta
        if (contexto.Request.Path.Value?.Contains("/Login", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                using var documento = System.Text.Json.JsonDocument.Parse(corpoResposta);
                if (documento.RootElement.TryGetProperty("SessionId", out var sessionId))
                    return sessionId.GetString();
            }
            catch { }
        }

        // Nas outras chamadas, vem no cookie B1SESSION
        if (contexto.Request.Cookies.TryGetValue("B1SESSION", out var cookieSessao))
            return cookieSessao;

        return null;
    }

    private static string? ExtrairEntidade(PathString caminho)
    {
        var partes = caminho.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (partes == null || partes.Length < 3) return null;
        var entidade = partes[2];
        var indiceParenteses = entidade.IndexOf('(');
        return indiceParenteses > 0 ? entidade[..indiceParenteses] : entidade;
    }
}
