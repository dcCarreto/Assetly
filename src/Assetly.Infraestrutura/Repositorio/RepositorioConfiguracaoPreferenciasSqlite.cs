using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Preferencias;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioConfiguracaoPreferenciasSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioConfiguracaoPreferencias
{
    private readonly FabricaConexaoSqlite _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");

    public ConfiguracaoPreferencias Obter()
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT * FROM configuracao_preferencias WHERE id = 1";
            using var resultado = comando.ExecuteReader();
            return resultado.Read() ? MapeadorConfiguracaoPreferencias.Mapear(resultado) : ConfiguracaoPreferencias.Padrao();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível obter as preferências", excecao);
        }
    }

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
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.AddWithValue("$tema", ConversorSqlite.EnumParaTexto(configuracao.Tema));
            comando.Parameters.AddWithValue("$corDestaque", ConversorSqlite.EnumParaTexto(configuracao.CorDestaque));
            comando.Parameters.AddWithValue("$densidadeTabela", ConversorSqlite.EnumParaTexto(configuracao.DensidadeTabela));
            comando.Parameters.AddWithValue("$intervaloAtualizacao", ConversorSqlite.EnumParaTexto(configuracao.IntervaloAtualizacao));
            comando.Parameters.AddWithValue("$ordemModulos", ConversorSqlite.ListaParaTexto(configuracao.OrdemModulos));
            comando.Parameters.AddWithValue("$modulosOcultos", ConversorSqlite.ListaParaTexto(configuracao.ModulosOcultos));
            comando.Parameters.AddWithValue("$moduloInicial", (object?)configuracao.ModuloInicial ?? DBNull.Value);
            comando.Parameters.AddWithValue("$atualizarAutomaticamente", configuracao.AtualizarAutomaticamente ? 1 : 0);
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível salvar as preferências", excecao);
        }
    }
}
