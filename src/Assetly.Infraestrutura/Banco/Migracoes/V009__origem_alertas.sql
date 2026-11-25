-- Registro que originou cada alerta automático (garantia, manutenção, convênio, conta): dois registros com a
-- mesma mensagem passam a gerar alertas distintos. Alertas antigos ficam com origem nula.
ALTER TABLE alertas ADD COLUMN origem_id TEXT;
