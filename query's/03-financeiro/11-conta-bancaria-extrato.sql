/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: ContaBancariaExtrato
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 03-conta-bancaria.sql (bloco 5) */
IF OBJECT_ID(N'dbo.ContaBancariaExtrato', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaBancariaExtrato
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ContaBancariaExtrato_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ContaBancariaId BIGINT NOT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Tipo NVARCHAR(50) NOT NULL,
        Valor DECIMAL(18,2) NOT NULL,
        CONSTRAINT PK_ContaBancariaExtrato PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* Origem: 03-conta-bancaria.sql (bloco 6) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContaBancariaExtrato_ContaBancaria_ContaBancariaId')
BEGIN
    ALTER TABLE dbo.ContaBancariaExtrato
        WITH CHECK ADD CONSTRAINT FK_ContaBancariaExtrato_ContaBancaria_ContaBancariaId
        FOREIGN KEY (ContaBancariaId) REFERENCES dbo.ContaBancaria (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 03-conta-bancaria.sql (bloco 7) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContaBancariaExtrato_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ContaBancariaExtrato
        WITH CHECK ADD CONSTRAINT FK_ContaBancariaExtrato_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 03-conta-bancaria.sql (bloco 8) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaBancariaExtrato_ContaBancariaId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ContaBancariaExtrato'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ContaBancariaExtrato_ContaBancariaId_DataHoraCadastro
        ON dbo.ContaBancariaExtrato (ContaBancariaId, DataHoraCadastro DESC);
END;
GO


