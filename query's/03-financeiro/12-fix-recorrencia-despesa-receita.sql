/*
Hotfix: recorrencia em Despesa/Receita para bases legadas
Garante colunas e constraints esperadas pelo backend atual.
*/

USE [Financeiro];
GO

IF OBJECT_ID(N'dbo.Despesa', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Despesa', N'Recorrencia') IS NULL
    BEGIN
        ALTER TABLE dbo.Despesa
            ADD Recorrencia NVARCHAR(20) NOT NULL
                CONSTRAINT DF_Despesa_Recorrencia DEFAULT (N'Unica');
    END

    IF COL_LENGTH(N'dbo.Despesa', N'RecorrenciaFixa') IS NULL
    BEGIN
        ALTER TABLE dbo.Despesa
            ADD RecorrenciaFixa BIT NOT NULL
                CONSTRAINT DF_Despesa_RecorrenciaFixa DEFAULT (0);
    END

    IF COL_LENGTH(N'dbo.Despesa', N'QuantidadeRecorrencia') IS NULL
    BEGIN
        ALTER TABLE dbo.Despesa ADD QuantidadeRecorrencia INT NULL;
    END

    IF COL_LENGTH(N'dbo.Despesa', N'DataEfetivacao') IS NULL
    BEGIN
        ALTER TABLE dbo.Despesa ADD DataEfetivacao DATETIME2(0) NULL;
    END

    UPDATE dbo.Despesa
    SET Recorrencia = N'Mensal',
        RecorrenciaFixa = 1
    WHERE Recorrencia = N'Fixa';

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
    BEGIN
        ALTER TABLE dbo.Despesa DROP CONSTRAINT CK_Despesa_Recorrencia;
    END

    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual'));

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
    BEGIN
        ALTER TABLE dbo.Despesa DROP CONSTRAINT CK_Despesa_RecorrenciaFixa;
    END

    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_RecorrenciaFixa
        CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0);

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
    BEGIN
        ALTER TABLE dbo.Despesa DROP CONSTRAINT CK_Despesa_QuantidadeRecorrencia;
    END

    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

IF OBJECT_ID(N'dbo.Receita', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Receita', N'Recorrencia') IS NULL
    BEGIN
        ALTER TABLE dbo.Receita
            ADD Recorrencia NVARCHAR(20) NOT NULL
                CONSTRAINT DF_Receita_Recorrencia DEFAULT (N'Unica');
    END

    IF COL_LENGTH(N'dbo.Receita', N'RecorrenciaFixa') IS NULL
    BEGIN
        ALTER TABLE dbo.Receita
            ADD RecorrenciaFixa BIT NOT NULL
                CONSTRAINT DF_Receita_RecorrenciaFixa DEFAULT (0);
    END

    IF COL_LENGTH(N'dbo.Receita', N'QuantidadeRecorrencia') IS NULL
    BEGIN
        ALTER TABLE dbo.Receita ADD QuantidadeRecorrencia INT NULL;
    END

    IF COL_LENGTH(N'dbo.Receita', N'DataEfetivacao') IS NULL
    BEGIN
        ALTER TABLE dbo.Receita ADD DataEfetivacao DATETIME2(0) NULL;
    END

    UPDATE dbo.Receita
    SET Recorrencia = N'Mensal',
        RecorrenciaFixa = 1
    WHERE Recorrencia = N'Fixa';

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
    BEGIN
        ALTER TABLE dbo.Receita DROP CONSTRAINT CK_Receita_Recorrencia;
    END

    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual'));

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
    BEGIN
        ALTER TABLE dbo.Receita DROP CONSTRAINT CK_Receita_RecorrenciaFixa;
    END

    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_RecorrenciaFixa
        CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0);

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
    BEGIN
        ALTER TABLE dbo.Receita DROP CONSTRAINT CK_Receita_QuantidadeRecorrencia;
    END

    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO
