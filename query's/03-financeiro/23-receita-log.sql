/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: ReceitaLog
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 06-receita.sql (bloco 39) */
IF OBJECT_ID(N'dbo.ReceitaLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceitaLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ReceitaLog_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ReceitaId BIGINT NOT NULL,
        Acao NVARCHAR(20) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_ReceitaLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ReceitaLog_Acao CHECK (Acao IN (N'Cadastro', N'Atualizacao', N'Exclusao'))
    );
END;
GO

/* Origem: 06-receita.sql (bloco 40) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaLog_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaLog
        WITH CHECK ADD CONSTRAINT FK_ReceitaLog_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 06-receita.sql (bloco 41) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaLog
        WITH CHECK ADD CONSTRAINT FK_ReceitaLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 06-receita.sql (bloco 42) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaLog_ReceitaId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ReceitaLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaLog_ReceitaId_DataHoraCadastro
        ON dbo.ReceitaLog (ReceitaId, DataHoraCadastro DESC);
END;
GO


