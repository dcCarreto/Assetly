-- Documento como comprovante de uma manutenção, despesa ou abastecimento (sem chave estrangeira: o registro pode ser de
-- três tabelas; se ele for excluído, o documento continua ligado ao bem).
ALTER TABLE documentos ADD COLUMN vinculo_tipo TEXT;

ALTER TABLE documentos ADD COLUMN vinculo_id TEXT;

CREATE INDEX IF NOT EXISTS idx_documentos_vinculo ON documentos(vinculo_id);
