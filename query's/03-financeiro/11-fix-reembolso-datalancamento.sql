/*
Hotfix: Reembolso - garantir colunas DataLancamento e Competencia, e remover legado DataSolicitacao
*/

IF OBJECT_ID(N'dbo.Reembolso', N'U') IS NULL
BEGIN
    THROW 50001, 'Tabela dbo.Reembolso nao encontrada.', 1;
END;
GO

IF COL_LENGTH(N'dbo.Reembolso', N'DataLancamento') IS NULL
BEGIN
    IF COL_LENGTH(N'dbo.Reembolso', N'DataSolicitacao') IS NOT NULL
    BEGIN
        EXEC sp_rename N'dbo.Reembolso.DataSolicitacao', N'DataLancamento', N'COLUMN';
    END
    ELSE
    BEGIN
        ALTER TABLE dbo.Reembolso
            ADD DataLancamento DATE NOT NULL
                CONSTRAINT DF_Reembolso_DataLancamento DEFAULT (CONVERT(date, SYSUTCDATETIME()));
    END
END;
GO

IF COL_LENGTH(N'dbo.Reembolso', N'Competencia') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso
        ADD Competencia CHAR(7) NOT NULL
            CONSTRAINT DF_Reembolso_Competencia DEFAULT (CONVERT(char(7), SYSUTCDATETIME(), 120));
END;
GO

UPDATE dbo.Reembolso
SET Competencia = CONVERT(char(7), DataLancamento, 120)
WHERE Competencia IS NULL OR Competencia = '';
GO

IF COL_LENGTH(N'dbo.Reembolso', N'DataLancamento') IS NOT NULL
   AND COL_LENGTH(N'dbo.Reembolso', N'DataSolicitacao') IS NOT NULL
BEGIN
    EXEC(N'UPDATE dbo.Reembolso
          SET DataLancamento = ISNULL(DataLancamento, DataSolicitacao)
          WHERE DataSolicitacao IS NOT NULL;');

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_DataSolicitacao' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
    BEGIN
        DROP INDEX IX_Reembolso_DataSolicitacao ON dbo.Reembolso;
    END

    ALTER TABLE dbo.Reembolso
        DROP COLUMN DataSolicitacao;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_DataLancamento' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_DataLancamento
        ON dbo.Reembolso (DataLancamento DESC, Id DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_Competencia' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_Competencia
        ON dbo.Reembolso (Competencia, Id DESC);
END;
GO
