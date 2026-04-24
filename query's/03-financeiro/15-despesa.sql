/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: Despesa
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 05-despesa.sql (bloco 2) */
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

/* Origem: 05-despesa.sql (bloco 3) */
IF COL_LENGTH('dbo.Despesa', 'AnexoDocumento') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Despesa DROP COLUMN AnexoDocumento;
END;
GO

/* Origem: 05-despesa.sql (bloco 4) */
IF COL_LENGTH('dbo.Despesa', 'Documentos') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Despesa DROP COLUMN Documentos;
END;
GO

/* Origem: 05-despesa.sql (bloco 5) */
IF COL_LENGTH('dbo.Despesa', 'QuantidadeRecorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD QuantidadeRecorrencia INT NULL;
END;
GO

/* Origem: 05-despesa.sql (bloco 6) */
IF COL_LENGTH('dbo.Despesa', 'ValorTotalRateioAmigos') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ValorTotalRateioAmigos DECIMAL(18,2) NULL;
END;
GO

/* Origem: 05-despesa.sql (bloco 7) */
IF COL_LENGTH('dbo.Despesa', 'TipoRateioAmigos') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD TipoRateioAmigos NVARCHAR(20) NULL;
END;
GO

/* Origem: 05-despesa.sql (bloco 8) */
IF COL_LENGTH('dbo.Despesa', 'DespesaRecorrenciaOrigemId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD DespesaRecorrenciaOrigemId BIGINT NULL;
END;
GO

/* Origem: 05-despesa.sql (bloco 9) */
IF COL_LENGTH('dbo.Despesa', 'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD DataEfetivacao DATETIME2(0) NULL;
END;
GO

/* Origem: 05-despesa.sql (bloco 10) */
IF COL_LENGTH('dbo.Despesa', 'Competencia') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa
        ADD Competencia CHAR(7) NOT NULL
            CONSTRAINT DF_Despesa_Competencia DEFAULT (CONVERT(char(7), SYSUTCDATETIME(), 120));
END;
GO

/* Origem: 05-despesa.sql (bloco 11) */
UPDATE dbo.Despesa
SET Competencia = CONVERT(char(7), DataLancamento, 120)
WHERE Competencia IS NULL OR Competencia = '';
GO

/* Origem: 05-despesa.sql (bloco 12) */
IF COL_LENGTH('dbo.Despesa', 'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ContaDestinoId BIGINT NULL;
END;
GO

/* Origem: 05-despesa.sql (bloco 13) */
IF COL_LENGTH('dbo.Despesa', 'ReceitaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ReceitaTransferenciaId BIGINT NULL;
END;
GO

/* Origem: 05-despesa.sql (bloco 14) */
IF COL_LENGTH('dbo.Despesa', 'Recorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa
        ADD Recorrencia NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Despesa_Recorrencia DEFAULT (N'Unica');
END;
GO

/* Origem: 05-despesa.sql (bloco 15) */
IF COL_LENGTH('dbo.Despesa', 'RecorrenciaFixa') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa
        ADD RecorrenciaFixa BIT NOT NULL
            CONSTRAINT DF_Despesa_RecorrenciaFixa DEFAULT (0);
END;
GO

/* Origem: 05-despesa.sql (bloco 16) */
UPDATE dbo.Despesa
SET
    Recorrencia = N'Mensal',
    RecorrenciaFixa = 1
WHERE Recorrencia = N'Fixa';
GO

/* Origem: 05-despesa.sql (bloco 17) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual'));
END;
GO

/* Origem: 05-despesa.sql (bloco 18) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_RecorrenciaFixa
        CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0);
END;
GO

/* Origem: 05-despesa.sql (bloco 19) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

/* Origem: 05-despesa.sql (bloco 20) */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_TipoDespesa' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        DROP CONSTRAINT CK_Despesa_TipoDespesa;
END;
GO

/* Origem: 05-despesa.sql (bloco 21) */
ALTER TABLE dbo.Despesa
    WITH CHECK ADD CONSTRAINT CK_Despesa_TipoDespesa
    CHECK (TipoDespesa IN (N'alimentacao', N'transporte', N'moradia', N'lazer', N'saude', N'educacao', N'servicos', N'impostos', N'seguros', N'assinaturas', N'viagens', N'vestuario', N'outros'));
GO

/* Origem: 05-despesa.sql (bloco 22) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_TipoRateioAmigos' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_TipoRateioAmigos
        CHECK (TipoRateioAmigos IS NULL OR TipoRateioAmigos IN (N'Comum', N'Igualitario'));
END;
GO

/* Origem: 05-despesa.sql (bloco 23) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 05-despesa.sql (bloco 24) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_UsuarioCadastroId_Status_DataVencimento' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_UsuarioCadastroId_Status_DataVencimento
        ON dbo.Despesa (UsuarioCadastroId, Status, DataVencimento);
END;
GO

/* Origem: 05-despesa.sql (bloco 25) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_Competencia' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_Competencia
        ON dbo.Despesa (Competencia, Id DESC);
END;
GO

/* Origem: 05-despesa.sql (bloco 26) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Despesa_DespesaRecorrenciaOrigemId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Despesa_DespesaRecorrenciaOrigemId
        FOREIGN KEY (DespesaRecorrenciaOrigemId) REFERENCES dbo.Despesa (Id);
END;
GO

/* Origem: 05-despesa.sql (bloco 27) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_DespesaRecorrenciaOrigemId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_DespesaRecorrenciaOrigemId
        ON dbo.Despesa (DespesaRecorrenciaOrigemId);
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 2) */
IF COL_LENGTH(N'dbo.Despesa', N'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD DataEfetivacao DATETIME2(0) NULL;
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 3) */
IF COL_LENGTH(N'dbo.Despesa', N'Recorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa
        ADD Recorrencia NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Despesa_Recorrencia DEFAULT (N'Unica');
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 4) */
IF COL_LENGTH(N'dbo.Despesa', N'RecorrenciaFixa') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa
        ADD RecorrenciaFixa BIT NOT NULL
            CONSTRAINT DF_Despesa_RecorrenciaFixa DEFAULT (0);
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 5) */
IF COL_LENGTH(N'dbo.Despesa', N'QuantidadeRecorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD QuantidadeRecorrencia INT NULL;
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 6) */
UPDATE dbo.Despesa
SET
    Recorrencia = N'Mensal',
    RecorrenciaFixa = 1
WHERE Recorrencia = N'Fixa';
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 7) */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa DROP CONSTRAINT CK_Despesa_Recorrencia;
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 8) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual'));
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 9) */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa DROP CONSTRAINT CK_Despesa_RecorrenciaFixa;
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 10) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_RecorrenciaFixa
        CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0);
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 11) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

/* Origem: 12-fix-recorrencia-despesa-receita.sql (bloco 2) */
IF OBJECT_ID(N'dbo.Despesa', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Despesa', N'Recorrencia') IS NULL
    BEGIN
        ALTER TABLE dbo.Despesa
            ADD Recorrencia NVARCHAR(20) NOT NULL
                CONSTRAINT DF_Despesa_Recorrencia DEFAULT (N'Unica');
    END

    IF COL_LENGTH(N'dbo.Despesa', N'RecorrenciaFixa') IS NULL
    BEGIN
        ALTER TABLE dbo.Despesa
            ADD RecorrenciaFixa BIT NOT NULL
                CONSTRAINT DF_Despesa_RecorrenciaFixa DEFAULT (0);
    END

    IF COL_LENGTH(N'dbo.Despesa', N'QuantidadeRecorrencia') IS NULL
    BEGIN
        ALTER TABLE dbo.Despesa ADD QuantidadeRecorrencia INT NULL;
    END

    IF COL_LENGTH(N'dbo.Despesa', N'DataEfetivacao') IS NULL
    BEGIN
        ALTER TABLE dbo.Despesa ADD DataEfetivacao DATETIME2(0) NULL;
    END

    UPDATE dbo.Despesa
    SET Recorrencia = N'Mensal',
        RecorrenciaFixa = 1
    WHERE Recorrencia = N'Fixa';

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
    BEGIN
        ALTER TABLE dbo.Despesa DROP CONSTRAINT CK_Despesa_Recorrencia;
    END

    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual'));

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
    BEGIN
        ALTER TABLE dbo.Despesa DROP CONSTRAINT CK_Despesa_RecorrenciaFixa;
    END

    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_RecorrenciaFixa
        CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0);

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
    BEGIN
        ALTER TABLE dbo.Despesa DROP CONSTRAINT CK_Despesa_QuantidadeRecorrencia;
    END

    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

/* Origem: 13-documento.sql (bloco 2) */
IF COL_LENGTH(N'dbo.Despesa', N'AnexoDocumento') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Despesa DROP COLUMN AnexoDocumento;
END;
GO

/* Origem: 13-documento.sql (bloco 3) */
IF COL_LENGTH(N'dbo.Despesa', N'Documentos') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Despesa DROP COLUMN Documentos;
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 16) */
IF COL_LENGTH(N'dbo.Despesa', N'DespesaOrigemId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD DespesaOrigemId BIGINT NULL;
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 17) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Despesa_DespesaOrigemId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Despesa_DespesaOrigemId
        FOREIGN KEY (DespesaOrigemId) REFERENCES dbo.Despesa (Id);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 18) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_DespesaOrigemId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_DespesaOrigemId
        ON dbo.Despesa (DespesaOrigemId);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 19) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_UsuarioCadastroId_Status_DespesaOrigemId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_UsuarioCadastroId_Status_DespesaOrigemId
        ON dbo.Despesa (UsuarioCadastroId, Status, DespesaOrigemId)
        INCLUDE (DataLancamento);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 20) */
IF COL_LENGTH(N'dbo.Despesa', N'DespesaRecorrenciaOrigemId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD DespesaRecorrenciaOrigemId BIGINT NULL;
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 21) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Despesa_DespesaRecorrenciaOrigemId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Despesa_DespesaRecorrenciaOrigemId
        FOREIGN KEY (DespesaRecorrenciaOrigemId) REFERENCES dbo.Despesa (Id);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 22) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_DespesaRecorrenciaOrigemId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_DespesaRecorrenciaOrigemId
        ON dbo.Despesa (DespesaRecorrenciaOrigemId);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 23) */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Status' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa DROP CONSTRAINT CK_Despesa_Status;
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 24) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Status' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_Status
        CHECK (Status IN (N'Pendente', N'Efetivada', N'Cancelada', N'PendenteAprovacao', N'Rejeitado'));
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 2) */
IF COL_LENGTH(N'dbo.Despesa', N'ContaBancariaId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ContaBancariaId BIGINT NULL;
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 3) */
IF COL_LENGTH(N'dbo.Despesa', N'CartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD CartaoId BIGINT NULL;
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 4) */
IF COL_LENGTH(N'dbo.Despesa', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ContaDestinoId BIGINT NULL;
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 6) */
IF COL_LENGTH(N'dbo.Despesa', N'ReceitaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ReceitaTransferenciaId BIGINT NULL;
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 9) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_ContaBancaria_ContaBancariaId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_ContaBancaria_ContaBancariaId
        FOREIGN KEY (ContaBancariaId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 10) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 12) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_ContaBancaria_ContaDestinoId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_ContaBancaria_ContaDestinoId
        FOREIGN KEY (ContaDestinoId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 14) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Receita_ReceitaTransferenciaId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Receita_ReceitaTransferenciaId
        FOREIGN KEY (ReceitaTransferenciaId) REFERENCES dbo.Receita (Id);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 16) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ContaBancariaId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ContaBancariaId ON dbo.Despesa (ContaBancariaId);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 17) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_CartaoId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_CartaoId ON dbo.Despesa (CartaoId);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 18) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ContaDestinoId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ContaDestinoId ON dbo.Despesa (ContaDestinoId);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 21) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ReceitaTransferenciaId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ReceitaTransferenciaId ON dbo.Despesa (ReceitaTransferenciaId);
END;
GO

/* Origem: 18-fix-conta-destino-despesa-receita.sql (bloco 3) */
IF COL_LENGTH(N'dbo.Despesa', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ContaDestinoId BIGINT NULL;
END;
GO

/* Origem: 18-fix-conta-destino-despesa-receita.sql (bloco 5) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_ContaBancaria_ContaDestinoId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_ContaBancaria_ContaDestinoId
        FOREIGN KEY (ContaDestinoId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

/* Origem: 18-fix-conta-destino-despesa-receita.sql (bloco 7) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ContaDestinoId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ContaDestinoId ON dbo.Despesa (ContaDestinoId);
END;
GO

/* Origem: 19-fix-ck-despesa-tipo-outros.sql (bloco 3) */
IF OBJECT_ID(N'dbo.Despesa', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1
               FROM sys.check_constraints
               WHERE parent_object_id = OBJECT_ID(N'dbo.Despesa')
                 AND name = N'CK_Despesa_TipoDespesa')
    BEGIN
        ALTER TABLE dbo.Despesa
            DROP CONSTRAINT CK_Despesa_TipoDespesa;
    END;

    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_TipoDespesa
        CHECK (TipoDespesa IN (N'alimentacao', N'transporte', N'moradia', N'lazer', N'saude', N'educacao', N'servicos', N'impostos', N'seguros', N'assinaturas', N'viagens', N'vestuario', N'outros'));
END;
GO

/* Origem: 19-fix-vinculo-transacao-entre-contas.sql (bloco 2) */
IF COL_LENGTH(N'dbo.Despesa', N'ReceitaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ReceitaTransferenciaId BIGINT NULL;
END;
GO

/* Origem: 19-fix-vinculo-transacao-entre-contas.sql (bloco 4) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Receita_ReceitaTransferenciaId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Receita_ReceitaTransferenciaId
        FOREIGN KEY (ReceitaTransferenciaId) REFERENCES dbo.Receita (Id);
END;
GO

/* Origem: 19-fix-vinculo-transacao-entre-contas.sql (bloco 6) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ReceitaTransferenciaId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ReceitaTransferenciaId ON dbo.Despesa (ReceitaTransferenciaId);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 4) */
IF COL_LENGTH(N'dbo.Despesa', N'FaturaCartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD FaturaCartaoId BIGINT NULL;
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 10) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_FaturaCartao_FaturaCartaoId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_FaturaCartao_FaturaCartaoId
        FOREIGN KEY (FaturaCartaoId) REFERENCES dbo.FaturaCartao (Id);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 16) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_FaturaCartaoId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_FaturaCartaoId
        ON dbo.Despesa (FaturaCartaoId);
END;
GO


