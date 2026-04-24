/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: DespesaLog
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 05-despesa.sql (bloco 44) */
IF OBJECT_ID(N'dbo.DespesaLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaLog_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        Acao NVARCHAR(20) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_DespesaLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_DespesaLog_Acao CHECK (Acao IN (N'Cadastro', N'Atualizacao', N'Exclusao'))
    );
END;
GO

/* Origem: 05-despesa.sql (bloco 45) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaLog_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaLog
        WITH CHECK ADD CONSTRAINT FK_DespesaLog_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 05-despesa.sql (bloco 46) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaLog
        WITH CHECK ADD CONSTRAINT FK_DespesaLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 05-despesa.sql (bloco 47) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaLog_DespesaId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.DespesaLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaLog_DespesaId_DataHoraCadastro
        ON dbo.DespesaLog (DespesaId, DataHoraCadastro DESC);
END;
GO


