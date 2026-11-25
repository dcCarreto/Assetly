-- Dados de inventário (seguro, boletim de ocorrência, garantia) e valor atual informado pelo usuário.
ALTER TABLE bens ADD COLUMN marca TEXT NOT NULL DEFAULT '';

ALTER TABLE bens ADD COLUMN modelo TEXT NOT NULL DEFAULT '';

ALTER TABLE bens ADD COLUMN numero_serie TEXT NOT NULL DEFAULT '';

ALTER TABLE bens ADD COLUMN loja TEXT NOT NULL DEFAULT '';

ALTER TABLE bens ADD COLUMN localizacao TEXT NOT NULL DEFAULT '';

ALTER TABLE bens ADD COLUMN valor_informado TEXT;

ALTER TABLE bens ADD COLUMN moeda_valor_informado TEXT;

ALTER TABLE bens ADD COLUMN valor_informado_em TEXT;

ALTER TABLE bens ADD COLUMN taxa_depreciacao TEXT;
