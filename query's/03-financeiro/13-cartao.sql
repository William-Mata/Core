/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: Cartao
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 04-cartao.sql (bloco 2) */
IF OBJECT_ID(N'dbo.Cartao', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cartao
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Cartao_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(150) NOT NULL,
        Bandeira NVARCHAR(100) NOT NULL,
        Tipo NVARCHAR(20) NOT NULL CONSTRAINT DF_Cartao_Tipo DEFAULT (N'Credito'),
        Limite DECIMAL(18,2) NULL,
        SaldoDisponivel DECIMAL(18,2) NOT NULL,
        DiaVencimento DATE NULL,
        DataVencimentoCartao DATE NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Cartao_Status DEFAULT (N'Ativo'),
        CONSTRAINT PK_Cartao PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Cartao_Tipo CHECK (Tipo IN (N'Credito', N'Debito')),
        CONSTRAINT CK_Cartao_Status CHECK (Status IN (N'Ativo', N'Inativo'))
    );
END;
GO

/* Origem: 04-cartao.sql (bloco 3) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Cartao_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Cartao
        WITH CHECK ADD CONSTRAINT FK_Cartao_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 04-cartao.sql (bloco 4) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Cartao_UsuarioCadastroId_Status' AND object_id = OBJECT_ID(N'dbo.Cartao'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Cartao_UsuarioCadastroId_Status
        ON dbo.Cartao (UsuarioCadastroId, Status);
END;
GO


