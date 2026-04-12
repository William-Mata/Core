/*
Ordem sugerida: 19
Objetivo: criar vinculo entre despesa e receita espelhadas de transacao entre contas.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

IF COL_LENGTH(N'dbo.Despesa', N'ReceitaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ReceitaTransferenciaId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'DespesaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD DespesaTransferenciaId BIGINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Receita_ReceitaTransferenciaId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Receita_ReceitaTransferenciaId
        FOREIGN KEY (ReceitaTransferenciaId) REFERENCES dbo.Receita (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Despesa_DespesaTransferenciaId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Despesa_DespesaTransferenciaId
        FOREIGN KEY (DespesaTransferenciaId) REFERENCES dbo.Despesa (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ReceitaTransferenciaId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ReceitaTransferenciaId ON dbo.Despesa (ReceitaTransferenciaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_DespesaTransferenciaId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_DespesaTransferenciaId ON dbo.Receita (DespesaTransferenciaId);
END;
GO
