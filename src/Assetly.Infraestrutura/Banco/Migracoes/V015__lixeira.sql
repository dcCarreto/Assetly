-- Lixeira dos bens excluídos: retrato (JSON no formato da exportação) do bem e de tudo o que era ligado a ele, por 30 dias.
CREATE TABLE IF NOT EXISTS lixeira (
    id TEXT PRIMARY KEY,
    descricao TEXT NOT NULL,
    excluido_em TEXT NOT NULL,
    conteudo TEXT NOT NULL,
    arquivos TEXT NOT NULL DEFAULT ''
);
