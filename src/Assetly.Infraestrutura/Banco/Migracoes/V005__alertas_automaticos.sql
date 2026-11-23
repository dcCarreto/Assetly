ALTER TABLE alertas ADD COLUMN automatico INTEGER NOT NULL DEFAULT 0;

UPDATE alertas SET automatico = 1
WHERE mensagem LIKE 'Garantia vencida em %'
   OR mensagem LIKE 'Garantia vence em %'
   OR mensagem LIKE 'Manutenção atrasada desde %'
   OR mensagem LIKE 'Manutenção próxima em %'
   OR mensagem = 'Nota fiscal ausente.';
