using Microsoft.EntityFrameworkCore;
using SendBoxFluid.Dominio.Entidades;

namespace SendBoxFluid.Infraestrutura.Persistencia;

public class SendBoxDbContext : DbContext
{
    public DbSet<SessaoIntegracao> Sessoes => Set<SessaoIntegracao>();
    public DbSet<RegistroRequisicao> Requisicoes => Set<RegistroRequisicao>();
    public DbSet<DocumentoArmazenado> Documentos => Set<DocumentoArmazenado>();
    public DbSet<ConfiguracaoNarwal> ConfiguracoesNarwal => Set<ConfiguracaoNarwal>();
    public DbSet<ContadorEntidade> Contadores => Set<ContadorEntidade>();

    public SendBoxDbContext(DbContextOptions<SendBoxDbContext> opcoes) : base(opcoes) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ============ SessaoIntegracao ============
        mb.Entity<SessaoIntegracao>(e =>
        {
            e.ToTable("sessoes");
            e.HasKey(s => s.CodigoSessao);
            e.Property(s => s.CodigoSessao).HasMaxLength(100);
            e.Property(s => s.Mensagem).HasColumnType("text");
            e.Property(s => s.PayloadEnviadoErp).HasColumnType("text");
            e.Property(s => s.RespostaErp).HasColumnType("text");
            e.Property(s => s.IdentificadorNegocio).HasMaxLength(200);
            e.Property(s => s.DadosOriginaisNarwal).HasColumnType("text");
            e.Property(s => s.TipoAcao).HasConversion<int>();
            e.Property(s => s.TipoErp).HasConversion<int>();
            e.Property(s => s.Resultado).HasConversion<int>();
            e.HasMany(s => s.Requisicoes)
                .WithOne()
                .HasForeignKey(r => r.CodigoSessao)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.DataInicio);
        });

        // ============ RegistroRequisicao ============
        mb.Entity<RegistroRequisicao>(e =>
        {
            e.ToTable("requisicoes");
            e.HasKey(r => r.Identificador);
            e.Property(r => r.Metodo).HasMaxLength(10);
            e.Property(r => r.Caminho).HasMaxLength(2000);
            e.Property(r => r.CodigoSessao).HasMaxLength(100);
            e.Property(r => r.CorpoRequisicao).HasColumnType("text");
            e.Property(r => r.CorpoResposta).HasColumnType("text");
            e.Property(r => r.Entidade).HasMaxLength(100);
        });

        // ============ DocumentoArmazenado ============
        mb.Entity<DocumentoArmazenado>(e =>
        {
            e.ToTable("documentos");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).ValueGeneratedOnAdd();
            e.Property(d => d.Entidade).HasMaxLength(100);
            e.Property(d => d.DadosJson).HasColumnType("text");
            e.HasIndex(d => d.Entidade);
        });

        // ============ ConfiguracaoNarwal ============
        mb.Entity<ConfiguracaoNarwal>(e =>
        {
            e.ToTable("configuracoes_narwal");
            e.HasKey(c => c.Cliente);
            e.Property(c => c.Cliente).HasMaxLength(100);
            e.Property(c => c.UrlNarwal).HasMaxLength(500);
            e.Property(c => c.Usuario).HasMaxLength(100);
            e.Property(c => c.Senha).HasMaxLength(500);
            e.Ignore(c => c.TokenAtual);
            e.Ignore(c => c.TokenExpiraEm);
        });

        // ============ ContadorEntidade ============
        mb.Entity<ContadorEntidade>(e =>
        {
            e.ToTable("contadores");
            e.HasKey(c => c.Nome);
            e.Property(c => c.Nome).HasMaxLength(50);
        });
    }
}

/// <summary>
/// Documento armazenado no SendBox (PurchaseOrder, Draft, etc).
/// Espelha o que o RepositorioDocumento guardava em memoria.
/// </summary>
public class DocumentoArmazenado
{
    public long Id { get; set; }
    public string Entidade { get; set; } = string.Empty;
    public string DadosJson { get; set; } = string.Empty;
}

/// <summary>
/// Contador atomico (DocEntry, JdtNum) persistido no banco.
/// </summary>
public class ContadorEntidade
{
    public string Nome { get; set; } = string.Empty;
    public int Valor { get; set; }
}
