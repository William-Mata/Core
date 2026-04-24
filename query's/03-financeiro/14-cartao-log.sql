/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: CartaoLog
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 04-cartao.sql (bloco 5) */
IF OBJECT_ID(N'dbo.CartaoLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CartaoLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_CartaoLog_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        CartaoId BIGINT NOT NULL,
        Acao NVARCHAR(20) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_CartaoLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_CartaoLog_Acao CHECK (Acao IN (N'Cadastro', N'Atualizacao', N'Exclusao'))
    );
END;
GO

/* Origem: 04-cartao.sql (bloco 6) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CartaoLog_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.CartaoLog
        WITH CHECK ADD CONSTRAINT FK_CartaoLog_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 04-cartao.sql (bloco 7) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CartaoLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.CartaoLog
        WITH CHECK ADD CONSTRAINT FK_CartaoLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 04-cartao.sql (bloco 8) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CartaoLog_CartaoId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.CartaoLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CartaoLog_CartaoId_DataHoraCadastro
        ON dbo.CartaoLog (CartaoId, DataHoraCadastro DESC);
END;
GO


