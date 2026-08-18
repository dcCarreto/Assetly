using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.Dados;

public sealed record AlterarStatusBemComando(BemId Id, StatusBem Status);
