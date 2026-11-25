-- Conta recorrente (condomínio, energia...): ao ser paga, gera a do mês seguinte, em vez de cópias antecipadas.
ALTER TABLE despesas ADD COLUMN recorrente INTEGER NOT NULL DEFAULT 0;
