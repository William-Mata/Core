/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: ReembolsoDespesa
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 07-reembolso.sql (bloco 12) */
IF OBJECT_ID(N'dbo.ReembolsoDespesa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReembolsoDespesa
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReembolsoDespesa PRIMARY KEY,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ReembolsoDespesa_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ReembolsoId BIGINT NOT NULL,
        DespesaId BIGINT NOT NULL,
        CONSTRAINT UQ_ReembolsoDespesa_DespesaId UNIQUE (DespesaId)
    );
END;
GO

/* Origem: 07-reembolso.sql (bloco 13) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReembolsoDespesa_Reembolso_ReembolsoId')
BEGIN
    ALTER TABLE dbo.ReembolsoDespesa
        WITH CHECK ADD CONSTRAINT FK_ReembolsoDespesa_Reembolso_ReembolsoId
        FOREIGN KEY (ReembolsoId) REFERENCES dbo.Reembolso (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 07-reembolso.sql (bloco 14) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReembolsoDespesa_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.ReembolsoDespesa
        WITH CHECK ADD CONSTRAINT FK_ReembolsoDespesa_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id);
END;
GO

/* Origem: 07-reembolso.sql (bloco 15) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReembolsoDespesa_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReembolsoDespesa
        WITH CHECK ADD CONSTRAINT FK_ReembolsoDespesa_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO


