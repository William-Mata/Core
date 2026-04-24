/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: ContaBancariaLog
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 03-conta-bancaria.sql (bloco 9) */
IF OBJECT_ID(N'dbo.ContaBancariaLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaBancariaLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ContaBancariaLog_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ContaBancariaId BIGINT NOT NULL,
        Acao NVARCHAR(20) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_ContaBancariaLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ContaBancariaLog_Acao CHECK (Acao IN (N'Cadastro', N'Atualizacao', N'Exclusao'))
    );
END;
GO

/* Origem: 03-conta-bancaria.sql (bloco 10) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContaBancariaLog_ContaBancaria_ContaBancariaId')
BEGIN
    ALTER TABLE dbo.ContaBancariaLog
        WITH CHECK ADD CONSTRAINT FK_ContaBancariaLog_ContaBancaria_ContaBancariaId
        FOREIGN KEY (ContaBancariaId) REFERENCES dbo.ContaBancaria (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 03-conta-bancaria.sql (bloco 11) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContaBancariaLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ContaBancariaLog
        WITH CHECK ADD CONSTRAINT FK_ContaBancariaLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 03-conta-bancaria.sql (bloco 12) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaBancariaLog_ContaBancariaId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ContaBancariaLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ContaBancariaLog_ContaBancariaId_DataHoraCadastro
        ON dbo.ContaBancariaLog (ContaBancariaId, DataHoraCadastro DESC);
END;
GO


