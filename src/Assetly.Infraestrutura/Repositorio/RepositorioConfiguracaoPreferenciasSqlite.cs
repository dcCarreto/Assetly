using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Preferencias;
using Assetly.Infraestrutura.Banco;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioConfiguracaoPreferenciasSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioConfiguracaoPreferencias
{
    private readonly ExecutorSqlite _executor = new(fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória"));

    public ConfiguracaoPreferencias Obter() =>
        _executor.Consultar("SELECT * FROM configuracao_preferencias WHERE id = 1", MapeadorConfiguracaoPreferencias.Mapear,
            "Não foi possível obter as preferências").FirstOrDefault() ?? ConfiguracaoPreferencias.Padrao();

    public void Salvar(ConfiguracaoPreferencias configuracao)
    {
        const string sql = """
            INSERT INTO configuracao_preferencias (id, tema, cor_destaque, densidade_tabela, intervalo_atualizacao, ordem_modulos, modulos_ocultos, modulo_inicial, atualizar_automaticamente)
            VALUES (1, $tema, $corDestaque, $densidadeTabela, $intervaloAtualizacao, $ordemModulos, $modulosOcultos, $moduloInicial, $atualizarAutomaticamente)
            ON CONFLICT(id) DO UPDATE SET
                tema = excluded.tema,
                cor_destaque = excluded.cor_destaque,
                densidade_tabela = excluded.densidade_tabela,
                intervalo_atualizacao = excluded.intervalo_atualizacao,
                ordem_modulos = excluded.ordem_modulos,
                modulos_ocultos = excluded.modulos_ocultos,
                modulo_inicial = excluded.modulo_inicial,
                atualizar_automaticamente = excluded.atualizar_automaticamente
            """;
        _executor.Executar(sql, "Não foi possível salvar as preferências",
            ("$tema", ConversorSqlite.EnumParaTexto(configuracao.Tema)),
            ("$corDestaque", ConversorSqlite.EnumParaTexto(configuracao.CorDestaque)),
            ("$densidadeTabela", ConversorSqlite.EnumParaTexto(configuracao.DensidadeTabela)),
            ("$intervaloAtualizacao", ConversorSqlite.EnumParaTexto(configuracao.IntervaloAtualizacao)),
            ("$ordemModulos", ConversorSqlite.ListaParaTexto(configuracao.OrdemModulos)),
            ("$modulosOcultos", ConversorSqlite.ListaParaTexto(configuracao.ModulosOcultos)),
            ("$moduloInicial", ConversorSqlite.Nulo(configuracao.ModuloInicial)),
            ("$atualizarAutomaticamente", configuracao.AtualizarAutomaticamente ? 1 : 0));
    }
}
