/*
Ordem sugerida: 06
Objetivo: criar objetos de receita, rateios e log.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

IF OBJECT_ID(N'dbo.Receita', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Receita
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Receita_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Observacao NVARCHAR(1000) NULL,
        DataLancamento DATE NOT NULL,
        DataVencimento DATE NOT NULL,
        DataEfetivacao DATE NULL,
        TipoReceita NVARCHAR(50) NOT NULL,
        TipoRecebimento NVARCHAR(50) NOT NULL,
        Recorrencia NVARCHAR(20) NOT NULL CONSTRAINT DF_Receita_Recorrencia DEFAULT (N'Unica'),
        RecorrenciaFixa BIT NOT NULL CONSTRAINT DF_Receita_RecorrenciaFixa DEFAULT (0),
        QuantidadeRecorrencia INT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        ValorLiquido DECIMAL(18,2) NOT NULL,
        Desconto DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Desconto DEFAULT (0),
        Acrescimo DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Acrescimo DEFAULT (0),
        Imposto DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Imposto DEFAULT (0),
        Juros DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Juros DEFAULT (0),
        ValorEfetivacao DECIMAL(18,2) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Receita_Status DEFAULT (N'Pendente'),
        ContaBancariaId BIGINT NULL,
        AnexoDocumento NVARCHAR(500) NULL,
        CONSTRAINT PK_Receita PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Receita_Recorrencia CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual')),
        CONSTRAINT CK_Receita_RecorrenciaFixa CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0),
        CONSTRAINT CK_Receita_QuantidadeRecorrencia CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0),
        CONSTRAINT CK_Receita_Status CHECK (Status IN (N'Pendente', N'Efetivada', N'Cancelada')),
        CONSTRAINT CK_Receita_TipoReceita CHECK (TipoReceita IN (N'salario', N'freelance', N'reembolso', N'investimento', N'bonus', N'outros')),
        CONSTRAINT CK_Receita_TipoRecebimento CHECK (TipoRecebimento IN (N'pix', N'transferencia', N'contaCorrente', N'dinheiro', N'boleto'))
    );
END;
GO

IF COL_LENGTH('dbo.Receita', 'QuantidadeRecorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD QuantidadeRecorrencia INT NULL;
END;
GO

IF COL_LENGTH('dbo.Receita', 'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD DataEfetivacao DATE NULL;
END;
GO

IF COL_LENGTH('dbo.Receita', 'Recorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Receita
        ADD Recorrencia NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Receita_Recorrencia DEFAULT (N'Unica');
END;
GO

IF COL_LENGTH('dbo.Receita', 'RecorrenciaFixa') IS NULL
BEGIN
    ALTER TABLE dbo.Receita
        ADD RecorrenciaFixa BIT NOT NULL
            CONSTRAINT DF_Receita_RecorrenciaFixa DEFAULT (0);
END;
GO

UPDATE dbo.Receita
SET
    Recorrencia = N'Mensal',
    RecorrenciaFixa = 1
WHERE Recorrencia = N'Fixa';
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_RecorrenciaFixa
        CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_ContaBancaria_ContaBancariaId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_ContaBancaria_ContaBancariaId
        FOREIGN KEY (ContaBancariaId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_UsuarioCadastroId_Status_DataVencimento' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_UsuarioCadastroId_Status_DataVencimento
        ON dbo.Receita (UsuarioCadastroId, Status, DataVencimento);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ContaBancariaId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_ContaBancariaId
        ON dbo.Receita (ContaBancariaId);
END;
GO

IF OBJECT_ID(N'dbo.ReceitaAmigoRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceitaAmigoRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ReceitaAmigoRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ReceitaId BIGINT NOT NULL,
        AmigoNome NVARCHAR(150) NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_ReceitaAmigoRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAmigoRateio_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAmigoRateio_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAmigoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAmigoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAmigoRateio_ReceitaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAmigoRateio_ReceitaId
        ON dbo.ReceitaAmigoRateio (ReceitaId);
END;
GO

IF OBJECT_ID(N'dbo.ReceitaAreaRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceitaAreaRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ReceitaAreaRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ReceitaId BIGINT NOT NULL,
        AreaId BIGINT NOT NULL,
        SubAreaId BIGINT NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_ReceitaAreaRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Area_AreaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Area_AreaId
        FOREIGN KEY (AreaId) REFERENCES dbo.Area (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_SubArea_SubAreaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_SubArea_SubAreaId
        FOREIGN KEY (SubAreaId) REFERENCES dbo.SubArea (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAreaRateio_ReceitaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAreaRateio_ReceitaId
        ON dbo.ReceitaAreaRateio (ReceitaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAreaRateio_AreaId_SubAreaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAreaRateio_AreaId_SubAreaId
        ON dbo.ReceitaAreaRateio (AreaId, SubAreaId);
END;
GO

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

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaLog_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaLog
        WITH CHECK ADD CONSTRAINT FK_ReceitaLog_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaLog
        WITH CHECK ADD CONSTRAINT FK_ReceitaLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaLog_ReceitaId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ReceitaLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaLog_ReceitaId_DataHoraCadastro
        ON dbo.ReceitaLog (ReceitaId, DataHoraCadastro DESC);
END;
GO
