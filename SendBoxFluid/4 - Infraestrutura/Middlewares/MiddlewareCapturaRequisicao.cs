using System.Text;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Enumeradores;
using SendBoxFluid.Dominio.Interfaces.Repositorios;
using SendBoxFluid.Dominio.Servicos;

namespace SendBoxFluid.Infraestrutura.Middlewares;

/// <summary>
/// Intercepta toda requisicao HTTP, captura corpo e resposta,
/// e registra na sessao correspondente (identificada pelo B1SESSION).
/// </summary>
public class MiddlewareCapturaRequisicao
{
    private readonly RequestDelegate _proximo;

    public MiddlewareCapturaRequisicao(RequestDelegate proximo)
    {
        _proximo = proximo;
    }

    public async Task InvokeAsync(HttpContext contexto, IRepositorioSessao repositorioSessao)
    {
        // Intercepta rotas dos ERPs (SAP B1, Sankhya)
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

            RegistrarSessao(contexto, corpoRequisicao, corpoResposta, repositorioSessao);
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

    private static void RegistrarSessao(
        HttpContext contexto,
        string corpoRequisicao,
        string corpoResposta,
        IRepositorioSessao repositorioSessao)
    {
        var codigoSessao = ExtrairCodigoSessao(contexto, corpoResposta);
        if (string.IsNullOrEmpty(codigoSessao))
            return;

        var entidade = ExtrairEntidade(contexto.Request.Path);
        var registro = new RegistroRequisicao(
            metodo: contexto.Request.Method,
            caminho: contexto.Request.Path + contexto.Request.QueryString,
            codigoSessao: codigoSessao,
            corpoRequisicao: corpoRequisicao,
            corpoResposta: corpoResposta,
            codigoStatusHttp: contexto.Response.StatusCode,
            entidade: entidade);

        var sessao = repositorioSessao.ObterOuCriar(codigoSessao);
        sessao.AdicionarRequisicao(registro);
        sessao.TipoErp = IdentificarErp(contexto.Request.Path);

        AtualizarTipoEResultadoSessao(sessao, registro);
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

    private static string? ExtrairCodigoSessao(HttpContext contexto, string corpoResposta)
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
        // /b1s/v1/Drafts -> Drafts
        var entidade = partes[2];
        var indiceParenteses = entidade.IndexOf('(');
        return indiceParenteses > 0 ? entidade[..indiceParenteses] : entidade;
    }

    private static void AtualizarTipoEResultadoSessao(SessaoIntegracao sessao, RegistroRequisicao registro)
    {
        var tipoAcao = ServicoIdentificadorAcao.Identificar(registro.Metodo, registro.Caminho, registro.CorpoRequisicao);

        // So atualiza o tipo da sessao se for uma acao "principal" (POST principal)
        if (tipoAcao != TipoAcaoEnum.Login &&
            tipoAcao != TipoAcaoEnum.ConsultaPedidoCompra &&
            tipoAcao != TipoAcaoEnum.ConsultaPedidoVenda &&
            tipoAcao != TipoAcaoEnum.ConsultaNotaFiscal &&
            tipoAcao != TipoAcaoEnum.Desconhecido)
        {
            sessao.TipoAcao = tipoAcao;
        }

        // Inferencia de resultado pelo status HTTP
        if (registro.CodigoStatusHttp >= 400)
        {
            sessao.Resultado = ResultadoIntegracaoEnum.Erro;
            sessao.Mensagem = $"HTTP {registro.CodigoStatusHttp} em {registro.Caminho}";
        }
        else if (sessao.Resultado == ResultadoIntegracaoEnum.EmAndamento &&
                 registro.Metodo.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                 !registro.Caminho.Contains("/Login", StringComparison.OrdinalIgnoreCase))
        {
            sessao.Resultado = ResultadoIntegracaoEnum.Sucesso;
            sessao.PayloadEnviadoErp = registro.CorpoRequisicao;
            sessao.RespostaErp = registro.CorpoResposta;

            // Extrai identificador de negocio (NfeId, ProcessoId, etc) do payload
            var identificador = ServicoExtratorIdentificador.Extrair(registro.CorpoRequisicao);
            if (!string.IsNullOrEmpty(identificador))
                sessao.IdentificadorNegocio = identificador;
        }
    }
}
