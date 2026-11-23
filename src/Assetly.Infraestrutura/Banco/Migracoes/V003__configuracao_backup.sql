CREATE TABLE IF NOT EXISTS configuracao_backup (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    caminho_destino TEXT,
    auto_backup_habilitado INTEGER NOT NULL DEFAULT 0,
    frequencia TEXT NOT NULL DEFAULT 'DIARIA',
    ultimo_backup_em TEXT
);
