CREATE TABLE IF NOT EXISTS configuracao_preferencias (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    tema TEXT NOT NULL DEFAULT 'ESCURO',
    cor_destaque TEXT NOT NULL DEFAULT 'CIANO',
    densidade_tabela TEXT NOT NULL DEFAULT 'CONFORTAVEL',
    intervalo_atualizacao TEXT NOT NULL DEFAULT 'SEGUNDOS5',
    ordem_modulos TEXT NOT NULL DEFAULT '',
    modulos_ocultos TEXT NOT NULL DEFAULT '',
    modulo_inicial TEXT
);
