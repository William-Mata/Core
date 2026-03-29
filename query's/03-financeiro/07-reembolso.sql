/*
Reembolso
*/

IF OBJECT_ID(N'dbo.Reembolso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reembolso
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reembolso PRIMARY KEY,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Reembolso_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Solicitante NVARCHAR(150) NOT NULL,
        DataLancamento DATE NOT NULL,
        DataEfetivacao DATE NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        CONSTRAINT CK_Reembolso_ValorTotal CHECK (ValorTotal >= 0),
        CONSTRAINT CK_Reembolso_Status CHECK (Status IN (N'Aguardando', N'Aprovado', N'Pago', N'Cancelado', N'Rejeitado'))
    );
END;
GO

IF COL_LENGTH(N'dbo.Reembolso', N'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso
        ADD DataEfetivacao DATE NULL;
END;
GO

IF COL_LENGTH(N'dbo.Reembolso', N'DataLancamento') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso
        ADD DataLancamento DATE NOT NULL
            CONSTRAINT DF_Reembolso_DataLancamento DEFAULT (CONVERT(date, SYSUTCDATETIME()));
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Reembolso_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Reembolso
        WITH CHECK ADD CONSTRAINT FK_Reembolso_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_DataLancamento' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_DataLancamento
        ON dbo.Reembolso (DataLancamento DESC, Id DESC);
END;
GO

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

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReembolsoDespesa_Reembolso_ReembolsoId')
BEGIN
    ALTER TABLE dbo.ReembolsoDespesa
        WITH CHECK ADD CONSTRAINT FK_ReembolsoDespesa_Reembolso_ReembolsoId
        FOREIGN KEY (ReembolsoId) REFERENCES dbo.Reembolso (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReembolsoDespesa_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.ReembolsoDespesa
        WITH CHECK ADD CONSTRAINT FK_ReembolsoDespesa_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReembolsoDespesa_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReembolsoDespesa
        WITH CHECK ADD CONSTRAINT FK_ReembolsoDespesa_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO
