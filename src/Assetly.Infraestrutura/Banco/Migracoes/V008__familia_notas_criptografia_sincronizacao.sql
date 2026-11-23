ALTER TABLE bens ADD COLUMN responsavel_id TEXT REFERENCES bens(id) ON DELETE SET NULL;

ALTER TABLE documentos ADD COLUMN chave_acesso TEXT NOT NULL DEFAULT '';

ALTER TABLE documentos ADD COLUMN url_consulta TEXT NOT NULL DEFAULT '';

ALTER TABLE configuracao_backup ADD COLUMN criptografado INTEGER NOT NULL DEFAULT 0;

ALTER TABLE configuracao_backup ADD COLUMN senha_protegida TEXT;

CREATE TABLE IF NOT EXISTS configuracao_sincronizacao (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    pasta TEXT,
    ativa INTEGER NOT NULL DEFAULT 0,
    ultima_impressao TEXT,
    ultima_sincronizacao_em TEXT
);

CREATE INDEX IF NOT EXISTS idx_bens_responsavel_id ON bens(responsavel_id);
