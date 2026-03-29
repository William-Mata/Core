/*
Ordem sugerida: 09
Objetivo: corrigir bases antigas que nao possuem colunas de efetivacao/recorrencia.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

IF COL_LENGTH(N'dbo.Despesa', N'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD DataEfetivacao DATE NULL;
END;
GO

IF COL_LENGTH(N'dbo.Despesa', N'Recorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa
        ADD Recorrencia NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Despesa_Recorrencia DEFAULT (N'Unica');
END;
GO

IF COL_LENGTH(N'dbo.Despesa', N'QuantidadeRecorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD QuantidadeRecorrencia INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual', N'Fixa'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD DataEfetivacao DATE NULL;
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'Recorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Receita
        ADD Recorrencia NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Receita_Recorrencia DEFAULT (N'Unica');
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'QuantidadeRecorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD QuantidadeRecorrencia INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual', N'Fixa'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

IF COL_LENGTH(N'dbo.Reembolso', N'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso ADD DataEfetivacao DATE NULL;
END;
GO
