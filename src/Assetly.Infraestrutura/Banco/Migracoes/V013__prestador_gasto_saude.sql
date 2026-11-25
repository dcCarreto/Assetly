-- Prestador e CPF/CNPJ do gasto de saúde: exigidos pela Receita para deduzir no IRPF.
ALTER TABLE gastos_saude ADD COLUMN prestador TEXT NOT NULL DEFAULT '';

ALTER TABLE gastos_saude ADD COLUMN documento_prestador TEXT NOT NULL DEFAULT '';
