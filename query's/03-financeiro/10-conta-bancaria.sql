/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: ContaBancaria
Fonte: query's/03-financeiro/03-conta-bancaria.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 03-conta-bancaria.sql (bloco 2) */
IF OBJECT_ID(N'dbo.ContaBancaria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaBancaria
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ContaBancaria_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(150) NOT NULL,
        Banco NVARCHAR(100) NOT NULL,
        Agencia NVARCHAR(30) NOT NULL,
        Numero NVARCHAR(30) NOT NULL,
        SaldoInicial DECIMAL(18,2) NOT NULL,
        SaldoAtual DECIMAL(18,2) NOT NULL,
        DataAbertura DATE NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_ContaBancaria_Status DEFAULT (N'Ativa'),
        CONSTRAINT PK_ContaBancaria PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ContaBancaria_Status CHECK (Status IN (N'Ativa', N'Inativa'))
    );
END;
GO

/* Origem: 03-conta-bancaria.sql (bloco 3) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContaBancaria_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ContaBancaria
        WITH CHECK ADD CONSTRAINT FK_ContaBancaria_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 03-conta-bancaria.sql (bloco 4) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaBancaria_UsuarioCadastroId_Status' AND object_id = OBJECT_ID(N'dbo.ContaBancaria'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ContaBancaria_UsuarioCadastroId_Status
        ON dbo.ContaBancaria (UsuarioCadastroId, Status);
END;
GO


