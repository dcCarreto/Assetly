using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Veiculo;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioVeiculoSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioVeiculo
{
    private readonly ExecutorSqlite _executor = new(fabricaConexao);

    public Veiculo? BuscarPorBem(BemId bemId) =>
        _executor.Consultar("SELECT * FROM veiculos WHERE bem_id = $bemId", Mapear, "Não foi possível buscar veículo",
            ("$bemId", bemId.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<Veiculo> Listar() =>
        _executor.Consultar("SELECT * FROM veiculos ORDER BY marca, modelo, placa", Mapear, "Não foi possível listar veículos");

    public void Salvar(Veiculo veiculo)
    {
        const string sql = """
            INSERT INTO veiculos (bem_id, placa, marca, modelo, ano_fabricacao, ano_modelo, cor, renavam, chassi, combustivel,
                quilometragem, proxima_revisao_km, proxima_revisao_em, vencimento_licenciamento, seguradora, apolice, vencimento_seguro)
            VALUES ($bemId, $placa, $marca, $modelo, $anoFabricacao, $anoModelo, $cor, $renavam, $chassi, $combustivel,
                $quilometragem, $proximaRevisaoKm, $proximaRevisaoEm, $vencimentoLicenciamento, $seguradora, $apolice, $vencimentoSeguro)
            ON CONFLICT(bem_id) DO UPDATE SET
                placa = excluded.placa, marca = excluded.marca, modelo = excluded.modelo,
                ano_fabricacao = excluded.ano_fabricacao, ano_modelo = excluded.ano_modelo, cor = excluded.cor,
                renavam = excluded.renavam, chassi = excluded.chassi, combustivel = excluded.combustivel,
                quilometragem = excluded.quilometragem, proxima_revisao_km = excluded.proxima_revisao_km,
                proxima_revisao_em = excluded.proxima_revisao_em, vencimento_licenciamento = excluded.vencimento_licenciamento,
                seguradora = excluded.seguradora, apolice = excluded.apolice, vencimento_seguro = excluded.vencimento_seguro
            """;
        _executor.Executar(sql, "Não foi possível salvar veículo",
            ("$bemId", veiculo.BemId.Valor.ToString()),
            ("$placa", veiculo.Placa),
            ("$marca", veiculo.Marca),
            ("$modelo", veiculo.Modelo),
            ("$anoFabricacao", ConversorSqlite.Nulo(veiculo.AnoFabricacao)),
            ("$anoModelo", ConversorSqlite.Nulo(veiculo.AnoModelo)),
            ("$cor", veiculo.Cor),
            ("$renavam", veiculo.Renavam),
            ("$chassi", veiculo.Chassi),
            ("$combustivel", ConversorSqlite.EnumParaTexto(veiculo.Combustivel)),
            ("$quilometragem", veiculo.Quilometragem),
            ("$proximaRevisaoKm", ConversorSqlite.Nulo(veiculo.ProximaRevisaoKm)),
            ("$proximaRevisaoEm", ConversorSqlite.Nulo(ConversorSqlite.Texto(veiculo.ProximaRevisaoEm))),
            ("$vencimentoLicenciamento", ConversorSqlite.Nulo(ConversorSqlite.Texto(veiculo.VencimentoLicenciamento))),
            ("$seguradora", veiculo.Seguradora),
            ("$apolice", veiculo.Apolice),
            ("$vencimentoSeguro", ConversorSqlite.Nulo(ConversorSqlite.Texto(veiculo.VencimentoSeguro))));
    }

    public void Excluir(BemId bemId) =>
        _executor.Executar("DELETE FROM veiculos WHERE bem_id = $bemId", "Não foi possível excluir veículo", ("$bemId", bemId.Valor.ToString()));

    private static Veiculo Mapear(SqliteDataReader leitor) =>
        Veiculo.Restaurar(
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("bem_id")))),
            new DadosVeiculo(
                ConversorSqlite.TextoColuna(leitor, "placa"),
                ConversorSqlite.TextoColuna(leitor, "marca"),
                ConversorSqlite.TextoColuna(leitor, "modelo"),
                ConversorSqlite.InteiroOpcional(leitor, "ano_fabricacao"),
                ConversorSqlite.InteiroOpcional(leitor, "ano_modelo"),
                ConversorSqlite.TextoColuna(leitor, "cor"),
                ConversorSqlite.TextoColuna(leitor, "renavam"),
                ConversorSqlite.TextoColuna(leitor, "chassi"),
                ConversorSqlite.EnumDeTexto<TipoCombustivel>(leitor.GetString(leitor.GetOrdinal("combustivel"))),
                ConversorSqlite.InteiroOpcional(leitor, "quilometragem") ?? 0,
                ConversorSqlite.InteiroOpcional(leitor, "proxima_revisao_km"),
                ConversorSqlite.Data(leitor, "proxima_revisao_em"),
                ConversorSqlite.Data(leitor, "vencimento_licenciamento"),
                ConversorSqlite.TextoColuna(leitor, "seguradora"),
                ConversorSqlite.TextoColuna(leitor, "apolice"),
                ConversorSqlite.Data(leitor, "vencimento_seguro")));
}
