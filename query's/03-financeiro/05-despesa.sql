/*
Ordem sugerida: 05
Objetivo: criar objetos de despesa, rateios e log.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

IF OBJECT_ID(N'dbo.Despesa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Despesa
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Despesa_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Observacao NVARCHAR(1000) NULL,
        Competencia CHAR(7) NOT NULL CONSTRAINT DF_Despesa_Competencia DEFAULT (CONVERT(char(7), SYSUTCDATETIME(), 120)),
        DataLancamento DATETIME2(0) NOT NULL,
        DataVencimento DATE NOT NULL,
        DataEfetivacao DATETIME2(0) NULL,
        TipoDespesa NVARCHAR(50) NOT NULL,
        TipoPagamento NVARCHAR(50) NOT NULL,
        Recorrencia NVARCHAR(20) NOT NULL CONSTRAINT DF_Despesa_Recorrencia DEFAULT (N'Unica'),
        RecorrenciaFixa BIT NOT NULL CONSTRAINT DF_Despesa_RecorrenciaFixa DEFAULT (0),
        QuantidadeRecorrencia INT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        ValorTotalRateioAmigos DECIMAL(18,2) NULL,
        TipoRateioAmigos NVARCHAR(20) NULL,
        ValorLiquido DECIMAL(18,2) NOT NULL,
        Desconto DECIMAL(18,2) NOT NULL CONSTRAINT DF_Despesa_Desconto DEFAULT (0),
        Acrescimo DECIMAL(18,2) NOT NULL CONSTRAINT DF_Despesa_Acrescimo DEFAULT (0),
        Imposto DECIMAL(18,2) NOT NULL CONSTRAINT DF_Despesa_Imposto DEFAULT (0),
        Juros DECIMAL(18,2) NOT NULL CONSTRAINT DF_Despesa_Juros DEFAULT (0),
        ValorEfetivacao DECIMAL(18,2) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Despesa_Status DEFAULT (N'Pendente'),
        ContaBancariaId BIGINT NULL,
        ContaDestinoId BIGINT NULL,
        ReceitaTransferenciaId BIGINT NULL,
        CartaoId BIGINT NULL,
        CONSTRAINT PK_Despesa PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Despesa_Recorrencia CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual')),
        CONSTRAINT CK_Despesa_RecorrenciaFixa CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0),
        CONSTRAINT CK_Despesa_QuantidadeRecorrencia CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0),
        CONSTRAINT CK_Despesa_Status CHECK (Status IN (N'Pendente', N'Efetivada', N'Cancelada')),
        CONSTRAINT CK_Despesa_TipoDespesa CHECK (TipoDespesa IN (N'alimentacao', N'transporte', N'moradia', N'lazer', N'saude', N'educacao', N'servicos', N'impostos', N'seguros', N'assinaturas', N'viagens', N'vestuario', N'outros')),
        CONSTRAINT CK_Despesa_TipoPagamento CHECK (TipoPagamento IN (N'pix', N'cartaoCredito', N'cartaoDebito', N'boleto', N'transferencia', N'dinheiro')),
        CONSTRAINT CK_Despesa_TipoRateioAmigos CHECK (TipoRateioAmigos IS NULL OR TipoRateioAmigos IN (N'Comum', N'Igualitario'))
    );
END;
GO

IF COL_LENGTH('dbo.Despesa', 'AnexoDocumento') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Despesa DROP COLUMN AnexoDocumento;
END;
GO

IF COL_LENGTH('dbo.Despesa', 'Documentos') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Despesa DROP COLUMN Documentos;
END;
GO

IF COL_LENGTH('dbo.Despesa', 'QuantidadeRecorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD QuantidadeRecorrencia INT NULL;
END;
GO

IF COL_LENGTH('dbo.Despesa', 'ValorTotalRateioAmigos') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ValorTotalRateioAmigos DECIMAL(18,2) NULL;
END;
GO

IF COL_LENGTH('dbo.Despesa', 'TipoRateioAmigos') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD TipoRateioAmigos NVARCHAR(20) NULL;
END;
GO

IF COL_LENGTH('dbo.Despesa', 'DespesaRecorrenciaOrigemId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD DespesaRecorrenciaOrigemId BIGINT NULL;
END;
GO

IF COL_LENGTH('dbo.Despesa', 'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD DataEfetivacao DATETIME2(0) NULL;
END;
GO

IF COL_LENGTH('dbo.Despesa', 'Competencia') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa
        ADD Competencia CHAR(7) NOT NULL
            CONSTRAINT DF_Despesa_Competencia DEFAULT (CONVERT(char(7), SYSUTCDATETIME(), 120));
END;
GO

UPDATE dbo.Despesa
SET Competencia = CONVERT(char(7), DataLancamento, 120)
WHERE Competencia IS NULL OR Competencia = '';
GO

IF COL_LENGTH('dbo.Despesa', 'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ContaDestinoId BIGINT NULL;
END;
GO

IF COL_LENGTH('dbo.Despesa', 'ReceitaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ReceitaTransferenciaId BIGINT NULL;
END;
GO

IF COL_LENGTH('dbo.Despesa', 'Recorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa
        ADD Recorrencia NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Despesa_Recorrencia DEFAULT (N'Unica');
END;
GO

IF COL_LENGTH('dbo.Despesa', 'RecorrenciaFixa') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa
        ADD RecorrenciaFixa BIT NOT NULL
            CONSTRAINT DF_Despesa_RecorrenciaFixa DEFAULT (0);
END;
GO

UPDATE dbo.Despesa
SET
    Recorrencia = N'Mensal',
    RecorrenciaFixa = 1
WHERE Recorrencia = N'Fixa';
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_RecorrenciaFixa
        CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_TipoDespesa' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        DROP CONSTRAINT CK_Despesa_TipoDespesa;
END;
GO

ALTER TABLE dbo.Despesa
    WITH CHECK ADD CONSTRAINT CK_Despesa_TipoDespesa
    CHECK (TipoDespesa IN (N'alimentacao', N'transporte', N'moradia', N'lazer', N'saude', N'educacao', N'servicos', N'impostos', N'seguros', N'assinaturas', N'viagens', N'vestuario', N'outros'));
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_TipoRateioAmigos' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_TipoRateioAmigos
        CHECK (TipoRateioAmigos IS NULL OR TipoRateioAmigos IN (N'Comum', N'Igualitario'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_UsuarioCadastroId_Status_DataVencimento' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_UsuarioCadastroId_Status_DataVencimento
        ON dbo.Despesa (UsuarioCadastroId, Status, DataVencimento);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_Competencia' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_Competencia
        ON dbo.Despesa (Competencia, Id DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Despesa_DespesaRecorrenciaOrigemId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Despesa_DespesaRecorrenciaOrigemId
        FOREIGN KEY (DespesaRecorrenciaOrigemId) REFERENCES dbo.Despesa (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_DespesaRecorrenciaOrigemId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_DespesaRecorrenciaOrigemId
        ON dbo.Despesa (DespesaRecorrenciaOrigemId);
END;
GO

IF OBJECT_ID(N'dbo.DespesaAmigoRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaAmigoRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaAmigoRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        AmigoNome NVARCHAR(150) NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_DespesaAmigoRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF COL_LENGTH('dbo.DespesaAmigoRateio', 'Valor') IS NULL
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio ADD Valor DECIMAL(18,2) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAmigoRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAmigoRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAmigoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAmigoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAmigoRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAmigoRateio_DespesaId
        ON dbo.DespesaAmigoRateio (DespesaId);
END;
GO

IF OBJECT_ID(N'dbo.DespesaAreaRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaAreaRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaAreaRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        AreaId BIGINT NOT NULL,
        SubAreaId BIGINT NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_DespesaAreaRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Area_AreaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Area_AreaId
        FOREIGN KEY (AreaId) REFERENCES dbo.Area (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_SubArea_SubAreaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_SubArea_SubAreaId
        FOREIGN KEY (SubAreaId) REFERENCES dbo.SubArea (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAreaRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAreaRateio_DespesaId
        ON dbo.DespesaAreaRateio (DespesaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAreaRateio_AreaId_SubAreaId' AND object_id = OBJECT_ID(N'dbo.DespesaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAreaRateio_AreaId_SubAreaId
        ON dbo.DespesaAreaRateio (AreaId, SubAreaId);
END;
GO

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

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaTipoRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaTipoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaTipoRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaTipoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaTipoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaTipoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaTipoRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaTipoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaTipoRateio_DespesaId
        ON dbo.DespesaTipoRateio (DespesaId);
END;
GO

IF OBJECT_ID(N'dbo.DespesaLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaLog_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        Acao NVARCHAR(20) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_DespesaLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_DespesaLog_Acao CHECK (Acao IN (N'Cadastro', N'Atualizacao', N'Exclusao'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaLog_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaLog
        WITH CHECK ADD CONSTRAINT FK_DespesaLog_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaLog
        WITH CHECK ADD CONSTRAINT FK_DespesaLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaLog_DespesaId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.DespesaLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaLog_DespesaId_DataHoraCadastro
        ON dbo.DespesaLog (DespesaId, DataHoraCadastro DESC);
END;
GO
