/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: Documento
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 13-documento.sql (bloco 6) */
IF OBJECT_ID(N'dbo.Documento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documento
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Documento PRIMARY KEY,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Documento_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        NomeArquivo NVARCHAR(260) NOT NULL,
        CaminhoArquivo NVARCHAR(1000) NOT NULL,
        ContentType NVARCHAR(200) NULL,
        TamanhoBytes BIGINT NOT NULL,
        DespesaId BIGINT NULL,
        ReceitaId BIGINT NULL,
        ReembolsoId BIGINT NULL,
        CONSTRAINT CK_Documento_VinculoUnico CHECK (
            (CASE WHEN DespesaId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN ReceitaId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN ReembolsoId IS NULL THEN 0 ELSE 1 END) = 1
        )
    );
END;
GO

/* Origem: 13-documento.sql (bloco 7) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Documento_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Documento
        WITH CHECK ADD CONSTRAINT FK_Documento_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 13-documento.sql (bloco 8) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Documento_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.Documento
        WITH CHECK ADD CONSTRAINT FK_Documento_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 13-documento.sql (bloco 9) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Documento_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.Documento
        WITH CHECK ADD CONSTRAINT FK_Documento_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 13-documento.sql (bloco 10) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Documento_Reembolso_ReembolsoId')
BEGIN
    ALTER TABLE dbo.Documento
        WITH CHECK ADD CONSTRAINT FK_Documento_Reembolso_ReembolsoId
        FOREIGN KEY (ReembolsoId) REFERENCES dbo.Reembolso (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 13-documento.sql (bloco 11) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documento_DespesaId' AND object_id = OBJECT_ID(N'dbo.Documento'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documento_DespesaId ON dbo.Documento (DespesaId);
END;
GO

/* Origem: 13-documento.sql (bloco 12) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documento_ReceitaId' AND object_id = OBJECT_ID(N'dbo.Documento'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documento_ReceitaId ON dbo.Documento (ReceitaId);
END;
GO

/* Origem: 13-documento.sql (bloco 13) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documento_ReembolsoId' AND object_id = OBJECT_ID(N'dbo.Documento'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documento_ReembolsoId ON dbo.Documento (ReembolsoId);
END;
GO


