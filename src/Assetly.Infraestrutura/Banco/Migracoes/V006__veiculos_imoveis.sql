CREATE TABLE IF NOT EXISTS veiculos (
    bem_id TEXT PRIMARY KEY,
    placa TEXT NOT NULL DEFAULT '',
    marca TEXT NOT NULL DEFAULT '',
    modelo TEXT NOT NULL DEFAULT '',
    ano_fabricacao INTEGER,
    ano_modelo INTEGER,
    cor TEXT NOT NULL DEFAULT '',
    renavam TEXT NOT NULL DEFAULT '',
    chassi TEXT NOT NULL DEFAULT '',
    combustivel TEXT NOT NULL,
    quilometragem INTEGER NOT NULL DEFAULT 0,
    proxima_revisao_km INTEGER,
    proxima_revisao_em TEXT,
    vencimento_licenciamento TEXT,
    seguradora TEXT NOT NULL DEFAULT '',
    apolice TEXT NOT NULL DEFAULT '',
    vencimento_seguro TEXT,
    FOREIGN KEY (bem_id) REFERENCES bens(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS abastecimentos (
    id TEXT PRIMARY KEY,
    bem_id TEXT NOT NULL,
    data TEXT NOT NULL,
    quilometragem INTEGER NOT NULL,
    litros TEXT NOT NULL,
    valor TEXT NOT NULL,
    moeda TEXT NOT NULL,
    combustivel TEXT NOT NULL,
    tanque_cheio INTEGER NOT NULL DEFAULT 0,
    posto TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (bem_id) REFERENCES bens(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS imoveis (
    bem_id TEXT PRIMARY KEY,
    tipo TEXT NOT NULL,
    situacao TEXT NOT NULL,
    logradouro TEXT NOT NULL DEFAULT '',
    numero TEXT NOT NULL DEFAULT '',
    complemento TEXT NOT NULL DEFAULT '',
    bairro TEXT NOT NULL DEFAULT '',
    cidade TEXT NOT NULL DEFAULT '',
    uf TEXT NOT NULL DEFAULT '',
    cep TEXT NOT NULL DEFAULT '',
    area_m2 TEXT,
    quartos INTEGER,
    banheiros INTEGER,
    vagas INTEGER,
    matricula TEXT NOT NULL DEFAULT '',
    inscricao_iptu TEXT NOT NULL DEFAULT '',
    condominio_valor TEXT,
    condominio_moeda TEXT,
    seguradora TEXT NOT NULL DEFAULT '',
    apolice TEXT NOT NULL DEFAULT '',
    vencimento_seguro TEXT,
    inquilino TEXT NOT NULL DEFAULT '',
    aluguel_valor TEXT,
    aluguel_moeda TEXT,
    fim_contrato_aluguel TEXT,
    FOREIGN KEY (bem_id) REFERENCES bens(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS despesas (
    id TEXT PRIMARY KEY,
    bem_id TEXT NOT NULL,
    tipo TEXT NOT NULL,
    descricao TEXT NOT NULL,
    vencimento TEXT NOT NULL,
    valor TEXT NOT NULL,
    moeda TEXT NOT NULL,
    paga_em TEXT,
    FOREIGN KEY (bem_id) REFERENCES bens(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_abastecimentos_bem_id ON abastecimentos(bem_id);
CREATE INDEX IF NOT EXISTS idx_despesas_bem_id ON despesas(bem_id);
CREATE INDEX IF NOT EXISTS idx_despesas_vencimento ON despesas(vencimento);
