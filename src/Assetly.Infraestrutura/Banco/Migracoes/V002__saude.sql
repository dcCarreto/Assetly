CREATE TABLE IF NOT EXISTS saude (
    id TEXT PRIMARY KEY,
    bem_id TEXT NOT NULL,
    tipo TEXT NOT NULL,
    fornecedor TEXT NOT NULL DEFAULT '',
    inicia_em TEXT NOT NULL,
    termina_em TEXT NOT NULL,
    contato TEXT NOT NULL DEFAULT '',
    data_cancelamento TEXT,
    FOREIGN KEY (bem_id) REFERENCES bens(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS gastos_saude (
    id TEXT PRIMARY KEY,
    bem_id TEXT NOT NULL,
    tipo TEXT NOT NULL,
    descricao TEXT NOT NULL,
    data TEXT NOT NULL,
    valor TEXT NOT NULL,
    moeda TEXT NOT NULL,
    FOREIGN KEY (bem_id) REFERENCES bens(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_saude_bem_id ON saude(bem_id);
CREATE INDEX IF NOT EXISTS idx_gastos_saude_bem_id ON gastos_saude(bem_id);
