/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: DespesaTipoRateio
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 05-despesa.sql (bloco 40) */
IF OBJECT_ID(N'dbo.DespesaTipoRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaTipoRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaTipoRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        TipoRateio NVARCHAR(100) NOT NULL,
        CONSTRAINT PK_DespesaTipoRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* Origem: 05-despesa.sql (bloco 41) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaTipoRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaTipoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaTipoRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 05-despesa.sql (bloco 42) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaTipoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaTipoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaTipoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 05-despesa.sql (bloco 43) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaTipoRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaTipoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaTipoRateio_DespesaId
        ON dbo.DespesaTipoRateio (DespesaId);
END;
GO


