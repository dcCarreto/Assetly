using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Imovel;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioImovelSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioImovel
{
    private readonly ExecutorSqlite _executor = new(fabricaConexao);

    public Imovel? BuscarPorBem(BemId bemId) =>
        _executor.Consultar("SELECT * FROM imoveis WHERE bem_id = $bemId", Mapear, "Não foi possível buscar imóvel",
            ("$bemId", bemId.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<Imovel> Listar() =>
        _executor.Consultar("SELECT * FROM imoveis ORDER BY cidade, logradouro, numero", Mapear, "Não foi possível listar imóveis");

    public void Salvar(Imovel imovel)
    {
        const string sql = """
            INSERT INTO imoveis (bem_id, tipo, situacao, logradouro, numero, complemento, bairro, cidade, uf, cep, area_m2, quartos,
                banheiros, vagas, matricula, inscricao_iptu, condominio_valor, condominio_moeda, seguradora, apolice, vencimento_seguro,
                inquilino, aluguel_valor, aluguel_moeda, fim_contrato_aluguel)
            VALUES ($bemId, $tipo, $situacao, $logradouro, $numero, $complemento, $bairro, $cidade, $uf, $cep, $areaM2, $quartos,
                $banheiros, $vagas, $matricula, $inscricaoIptu, $condominioValor, $condominioMoeda, $seguradora, $apolice, $vencimentoSeguro,
                $inquilino, $aluguelValor, $aluguelMoeda, $fimContratoAluguel)
            ON CONFLICT(bem_id) DO UPDATE SET
                tipo = excluded.tipo, situacao = excluded.situacao, logradouro = excluded.logradouro, numero = excluded.numero,
                complemento = excluded.complemento, bairro = excluded.bairro, cidade = excluded.cidade, uf = excluded.uf, cep = excluded.cep,
                area_m2 = excluded.area_m2, quartos = excluded.quartos, banheiros = excluded.banheiros, vagas = excluded.vagas,
                matricula = excluded.matricula, inscricao_iptu = excluded.inscricao_iptu, condominio_valor = excluded.condominio_valor,
                condominio_moeda = excluded.condominio_moeda, seguradora = excluded.seguradora, apolice = excluded.apolice,
                vencimento_seguro = excluded.vencimento_seguro, inquilino = excluded.inquilino, aluguel_valor = excluded.aluguel_valor,
                aluguel_moeda = excluded.aluguel_moeda, fim_contrato_aluguel = excluded.fim_contrato_aluguel
            """;
        _executor.Executar(sql, "Não foi possível salvar imóvel",
            ("$bemId", imovel.BemId.Valor.ToString()),
            ("$tipo", ConversorSqlite.EnumParaTexto(imovel.Tipo)),
            ("$situacao", ConversorSqlite.EnumParaTexto(imovel.Situacao)),
            ("$logradouro", imovel.Logradouro),
            ("$numero", imovel.Numero),
            ("$complemento", imovel.Complemento),
            ("$bairro", imovel.Bairro),
            ("$cidade", imovel.Cidade),
            ("$uf", imovel.Uf),
            ("$cep", imovel.Cep),
            ("$areaM2", ConversorSqlite.Nulo(ConversorSqlite.Texto(imovel.AreaM2))),
            ("$quartos", ConversorSqlite.Nulo(imovel.Quartos)),
            ("$banheiros", ConversorSqlite.Nulo(imovel.Banheiros)),
            ("$vagas", ConversorSqlite.Nulo(imovel.Vagas)),
            ("$matricula", imovel.Matricula),
            ("$inscricaoIptu", imovel.InscricaoIptu),
            ("$condominioValor", ConversorSqlite.Nulo(ConversorSqlite.Valor(imovel.ValorCondominio))),
            ("$condominioMoeda", ConversorSqlite.Nulo(ConversorSqlite.Moeda(imovel.ValorCondominio))),
            ("$seguradora", imovel.Seguradora),
            ("$apolice", imovel.Apolice),
            ("$vencimentoSeguro", ConversorSqlite.Nulo(ConversorSqlite.Texto(imovel.VencimentoSeguro))),
            ("$inquilino", imovel.Inquilino),
            ("$aluguelValor", ConversorSqlite.Nulo(ConversorSqlite.Valor(imovel.ValorAluguel))),
            ("$aluguelMoeda", ConversorSqlite.Nulo(ConversorSqlite.Moeda(imovel.ValorAluguel))),
            ("$fimContratoAluguel", ConversorSqlite.Nulo(ConversorSqlite.Texto(imovel.FimContratoAluguel))));
    }

    public void Excluir(BemId bemId) =>
        _executor.Executar("DELETE FROM imoveis WHERE bem_id = $bemId", "Não foi possível excluir imóvel", ("$bemId", bemId.Valor.ToString()));

    private static Imovel Mapear(SqliteDataReader leitor) =>
        Imovel.Restaurar(
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("bem_id")))),
            new DadosImovel(
                ConversorSqlite.EnumDeTexto<TipoImovel>(leitor.GetString(leitor.GetOrdinal("tipo"))),
                ConversorSqlite.EnumDeTexto<SituacaoImovel>(leitor.GetString(leitor.GetOrdinal("situacao"))),
                ConversorSqlite.TextoColuna(leitor, "logradouro"),
                ConversorSqlite.TextoColuna(leitor, "numero"),
                ConversorSqlite.TextoColuna(leitor, "complemento"),
                ConversorSqlite.TextoColuna(leitor, "bairro"),
                ConversorSqlite.TextoColuna(leitor, "cidade"),
                ConversorSqlite.TextoColuna(leitor, "uf"),
                ConversorSqlite.TextoColuna(leitor, "cep"),
                ConversorSqlite.DecimalOpcional(leitor, "area_m2"),
                ConversorSqlite.InteiroOpcional(leitor, "quartos"),
                ConversorSqlite.InteiroOpcional(leitor, "banheiros"),
                ConversorSqlite.InteiroOpcional(leitor, "vagas"),
                ConversorSqlite.TextoColuna(leitor, "matricula"),
                ConversorSqlite.TextoColuna(leitor, "inscricao_iptu"),
                ConversorSqlite.LerDinheiro(leitor, "condominio_valor", "condominio_moeda"),
                ConversorSqlite.TextoColuna(leitor, "seguradora"),
                ConversorSqlite.TextoColuna(leitor, "apolice"),
                ConversorSqlite.Data(leitor, "vencimento_seguro"),
                ConversorSqlite.TextoColuna(leitor, "inquilino"),
                ConversorSqlite.LerDinheiro(leitor, "aluguel_valor", "aluguel_moeda"),
                ConversorSqlite.Data(leitor, "fim_contrato_aluguel")));
}
