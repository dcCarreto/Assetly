-- Quantos backups manter no destino (os mais antigos são apagados depois de cada backup). 0 = manter todos:
-- é o valor de quem já usava o programa, para nenhum backup existente ser apagado sem o usuário escolher.
ALTER TABLE configuracao_backup ADD COLUMN manter_ultimos INTEGER NOT NULL DEFAULT 0;
