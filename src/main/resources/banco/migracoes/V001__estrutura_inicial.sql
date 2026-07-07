CREATE TABLE IF NOT EXISTS bens (
    id TEXT PRIMARY KEY,
    nome TEXT NOT NULL,
    tipo TEXT NOT NULL,
    status TEXT NOT NULL,
    comprado_em TEXT,
    preco_compra TEXT,
    moeda TEXT,
    observacoes TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS garantias (
    id TEXT PRIMARY KEY,
    bem_id TEXT NOT NULL,
    tipo TEXT NOT NULL,
    fornecedor TEXT NOT NULL DEFAULT '',
    inicia_em TEXT NOT NULL,
    termina_em TEXT NOT NULL,
    contato_suporte TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (bem_id) REFERENCES bens(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS manutencoes (
    id TEXT PRIMARY KEY,
    bem_id TEXT NOT NULL,
    tipo TEXT NOT NULL,
    descricao TEXT NOT NULL,
    agendada_para TEXT NOT NULL,
    concluida_em TEXT,
    custo TEXT,
    moeda TEXT,
    status TEXT NOT NULL,
    FOREIGN KEY (bem_id) REFERENCES bens(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS documentos (
    id TEXT PRIMARY KEY,
    bem_id TEXT NOT NULL,
    tipo TEXT NOT NULL,
    nome TEXT NOT NULL,
    caminho_local TEXT NOT NULL,
    registrado_em TEXT NOT NULL,
    status TEXT NOT NULL,
    FOREIGN KEY (bem_id) REFERENCES bens(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS alertas (
    id TEXT PRIMARY KEY,
    bem_id TEXT NOT NULL,
    tipo TEXT NOT NULL,
    severidade TEXT NOT NULL,
    mensagem TEXT NOT NULL,
    criado_em TEXT NOT NULL,
    prazo_em TEXT,
    status TEXT NOT NULL,
    FOREIGN KEY (bem_id) REFERENCES bens(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_garantias_bem_id ON garantias(bem_id);
CREATE INDEX IF NOT EXISTS idx_manutencoes_bem_id ON manutencoes(bem_id);
CREATE INDEX IF NOT EXISTS idx_documentos_bem_id ON documentos(bem_id);
CREATE INDEX IF NOT EXISTS idx_alertas_bem_id ON alertas(bem_id);
