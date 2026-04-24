/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: Receita
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 06-receita.sql (bloco 2) */
IF OBJECT_ID(N'dbo.Receita', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Receita
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Receita_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Observacao NVARCHAR(1000) NULL,
        Competencia CHAR(7) NOT NULL CONSTRAINT DF_Receita_Competencia DEFAULT (CONVERT(char(7), SYSUTCDATETIME(), 120)),
        DataLancamento DATETIME2(0) NOT NULL,
        DataVencimento DATE NOT NULL,
        DataEfetivacao DATETIME2(0) NULL,
        TipoReceita NVARCHAR(50) NOT NULL,
        TipoRecebimento NVARCHAR(50) NOT NULL,
        Recorrencia NVARCHAR(20) NOT NULL CONSTRAINT DF_Receita_Recorrencia DEFAULT (N'Unica'),
        RecorrenciaFixa BIT NOT NULL CONSTRAINT DF_Receita_RecorrenciaFixa DEFAULT (0),
        QuantidadeRecorrencia INT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        ValorTotalRateioAmigos DECIMAL(18,2) NULL,
        TipoRateioAmigos NVARCHAR(20) NULL,
        ValorLiquido DECIMAL(18,2) NOT NULL,
        Desconto DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Desconto DEFAULT (0),
        Acrescimo DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Acrescimo DEFAULT (0),
        Imposto DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Imposto DEFAULT (0),
        Juros DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Juros DEFAULT (0),
        ValorEfetivacao DECIMAL(18,2) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Receita_Status DEFAULT (N'Pendente'),
        ContaBancariaId BIGINT NULL,
        ContaDestinoId BIGINT NULL,
        DespesaTransferenciaId BIGINT NULL,
        CartaoId BIGINT NULL,
        CONSTRAINT PK_Receita PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Receita_Recorrencia CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual')),
        CONSTRAINT CK_Receita_RecorrenciaFixa CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0),
        CONSTRAINT CK_Receita_QuantidadeRecorrencia CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0),
        CONSTRAINT CK_Receita_Status CHECK (Status IN (N'Pendente', N'Efetivada', N'Cancelada')),
        CONSTRAINT CK_Receita_TipoReceita CHECK (TipoReceita IN (N'salario', N'freelance', N'reembolso', N'investimento', N'bonus', N'vendas', N'alugueis', N'beneficios', N'rendasExtras', N'outros')),
        CONSTRAINT CK_Receita_TipoRecebimento CHECK (TipoRecebimento IN (N'pix', N'transferencia', N'dinheiro', N'boleto', N'cartaoCredito', N'cartaoDebito')),
        CONSTRAINT CK_Receita_TipoRateioAmigos CHECK (TipoRateioAmigos IS NULL OR TipoRateioAmigos IN (N'Comum', N'Igualitario'))
    );
END;
GO

/* Origem: 06-receita.sql (bloco 3) */
UPDATE dbo.Receita
SET TipoRecebimento = N'transferencia'
WHERE TipoRecebimento = N'contaCorrente';
GO

/* Origem: 06-receita.sql (bloco 4) */
IF COL_LENGTH('dbo.Receita', 'AnexoDocumento') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Receita DROP COLUMN AnexoDocumento;
END;
GO

/* Origem: 06-receita.sql (bloco 5) */
IF COL_LENGTH('dbo.Receita', 'Documentos') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Receita DROP COLUMN Documentos;
END;
GO

/* Origem: 06-receita.sql (bloco 6) */
IF COL_LENGTH('dbo.Receita', 'QuantidadeRecorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD QuantidadeRecorrencia INT NULL;
END;
GO

/* Origem: 06-receita.sql (bloco 7) */
IF COL_LENGTH('dbo.Receita', 'ValorTotalRateioAmigos') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD ValorTotalRateioAmigos DECIMAL(18,2) NULL;
END;
GO

/* Origem: 06-receita.sql (bloco 8) */
IF COL_LENGTH('dbo.Receita', 'TipoRateioAmigos') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD TipoRateioAmigos NVARCHAR(20) NULL;
END;
GO

/* Origem: 06-receita.sql (bloco 9) */
IF COL_LENGTH('dbo.Receita', 'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD DataEfetivacao DATETIME2(0) NULL;
END;
GO

/* Origem: 06-receita.sql (bloco 10) */
IF COL_LENGTH('dbo.Receita', 'Competencia') IS NULL
BEGIN
    ALTER TABLE dbo.Receita
        ADD Competencia CHAR(7) NOT NULL
            CONSTRAINT DF_Receita_Competencia DEFAULT (CONVERT(char(7), SYSUTCDATETIME(), 120));
END;
GO

/* Origem: 06-receita.sql (bloco 11) */
UPDATE dbo.Receita
SET Competencia = CONVERT(char(7), DataLancamento, 120)
WHERE Competencia IS NULL OR Competencia = '';
GO

/* Origem: 06-receita.sql (bloco 12) */
IF COL_LENGTH('dbo.Receita', 'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD ContaDestinoId BIGINT NULL;
END;
GO

/* Origem: 06-receita.sql (bloco 13) */
IF COL_LENGTH('dbo.Receita', 'DespesaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD DespesaTransferenciaId BIGINT NULL;
END;
GO

/* Origem: 06-receita.sql (bloco 14) */
IF COL_LENGTH('dbo.Receita', 'Recorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Receita
        ADD Recorrencia NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Receita_Recorrencia DEFAULT (N'Unica');
END;
GO

/* Origem: 06-receita.sql (bloco 15) */
IF COL_LENGTH('dbo.Receita', 'RecorrenciaFixa') IS NULL
BEGIN
    ALTER TABLE dbo.Receita
        ADD RecorrenciaFixa BIT NOT NULL
            CONSTRAINT DF_Receita_RecorrenciaFixa DEFAULT (0);
END;
GO

/* Origem: 06-receita.sql (bloco 16) */
UPDATE dbo.Receita
SET
    Recorrencia = N'Mensal',
    RecorrenciaFixa = 1
WHERE Recorrencia = N'Fixa';
GO

/* Origem: 06-receita.sql (bloco 17) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual'));
END;
GO

/* Origem: 06-receita.sql (bloco 18) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_RecorrenciaFixa
        CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0);
END;
GO

/* Origem: 06-receita.sql (bloco 19) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

/* Origem: 06-receita.sql (bloco 20) */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_TipoReceita' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        DROP CONSTRAINT CK_Receita_TipoReceita;
END;
GO

/* Origem: 06-receita.sql (bloco 21) */
ALTER TABLE dbo.Receita
    WITH CHECK ADD CONSTRAINT CK_Receita_TipoReceita
    CHECK (TipoReceita IN (N'salario', N'freelance', N'reembolso', N'investimento', N'bonus', N'vendas', N'alugueis', N'beneficios', N'rendasExtras', N'outros'));
GO

/* Origem: 06-receita.sql (bloco 22) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_TipoRateioAmigos' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_TipoRateioAmigos
        CHECK (TipoRateioAmigos IS NULL OR TipoRateioAmigos IN (N'Comum', N'Igualitario'));
END;
GO

/* Origem: 06-receita.sql (bloco 23) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 06-receita.sql (bloco 24) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_ContaBancaria_ContaBancariaId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_ContaBancaria_ContaBancariaId
        FOREIGN KEY (ContaBancariaId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

/* Origem: 06-receita.sql (bloco 25) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_UsuarioCadastroId_Status_DataVencimento' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_UsuarioCadastroId_Status_DataVencimento
        ON dbo.Receita (UsuarioCadastroId, Status, DataVencimento);
END;
GO

/* Origem: 06-receita.sql (bloco 26) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_Competencia' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_Competencia
        ON dbo.Receita (Competencia, Id DESC);
END;
GO

/* Origem: 06-receita.sql (bloco 27) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ContaBancariaId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_ContaBancariaId
        ON dbo.Receita (ContaBancariaId);
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 12) */
IF COL_LENGTH(N'dbo.Receita', N'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD DataEfetivacao DATETIME2(0) NULL;
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 13) */
IF COL_LENGTH(N'dbo.Receita', N'Recorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Receita
        ADD Recorrencia NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Receita_Recorrencia DEFAULT (N'Unica');
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 14) */
IF COL_LENGTH(N'dbo.Receita', N'RecorrenciaFixa') IS NULL
BEGIN
    ALTER TABLE dbo.Receita
        ADD RecorrenciaFixa BIT NOT NULL
            CONSTRAINT DF_Receita_RecorrenciaFixa DEFAULT (0);
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 15) */
IF COL_LENGTH(N'dbo.Receita', N'QuantidadeRecorrencia') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD QuantidadeRecorrencia INT NULL;
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 16) */
UPDATE dbo.Receita
SET
    Recorrencia = N'Mensal',
    RecorrenciaFixa = 1
WHERE Recorrencia = N'Fixa';
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 17) */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita DROP CONSTRAINT CK_Receita_Recorrencia;
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 18) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual'));
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 19) */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita DROP CONSTRAINT CK_Receita_RecorrenciaFixa;
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 20) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_RecorrenciaFixa
        CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0);
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 21) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

/* Origem: 12-fix-recorrencia-despesa-receita.sql (bloco 3) */
IF OBJECT_ID(N'dbo.Receita', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Receita', N'Recorrencia') IS NULL
    BEGIN
        ALTER TABLE dbo.Receita
            ADD Recorrencia NVARCHAR(20) NOT NULL
                CONSTRAINT DF_Receita_Recorrencia DEFAULT (N'Unica');
    END

    IF COL_LENGTH(N'dbo.Receita', N'RecorrenciaFixa') IS NULL
    BEGIN
        ALTER TABLE dbo.Receita
            ADD RecorrenciaFixa BIT NOT NULL
                CONSTRAINT DF_Receita_RecorrenciaFixa DEFAULT (0);
    END

    IF COL_LENGTH(N'dbo.Receita', N'QuantidadeRecorrencia') IS NULL
    BEGIN
        ALTER TABLE dbo.Receita ADD QuantidadeRecorrencia INT NULL;
    END

    IF COL_LENGTH(N'dbo.Receita', N'DataEfetivacao') IS NULL
    BEGIN
        ALTER TABLE dbo.Receita ADD DataEfetivacao DATETIME2(0) NULL;
    END

    UPDATE dbo.Receita
    SET Recorrencia = N'Mensal',
        RecorrenciaFixa = 1
    WHERE Recorrencia = N'Fixa';

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Recorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
    BEGIN
        ALTER TABLE dbo.Receita DROP CONSTRAINT CK_Receita_Recorrencia;
    END

    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_Recorrencia
        CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual'));

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_RecorrenciaFixa' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
    BEGIN
        ALTER TABLE dbo.Receita DROP CONSTRAINT CK_Receita_RecorrenciaFixa;
    END

    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_RecorrenciaFixa
        CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0);

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_QuantidadeRecorrencia' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
    BEGIN
        ALTER TABLE dbo.Receita DROP CONSTRAINT CK_Receita_QuantidadeRecorrencia;
    END

    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_QuantidadeRecorrencia
        CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0);
END;
GO

/* Origem: 13-documento.sql (bloco 4) */
IF COL_LENGTH(N'dbo.Receita', N'AnexoDocumento') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Receita DROP COLUMN AnexoDocumento;
END;
GO

/* Origem: 13-documento.sql (bloco 5) */
IF COL_LENGTH(N'dbo.Receita', N'Documentos') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Receita DROP COLUMN Documentos;
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 25) */
IF COL_LENGTH(N'dbo.Receita', N'ReceitaOrigemId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD ReceitaOrigemId BIGINT NULL;
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 26) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Receita_ReceitaOrigemId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Receita_ReceitaOrigemId
        FOREIGN KEY (ReceitaOrigemId) REFERENCES dbo.Receita (Id);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 27) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ReceitaOrigemId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_ReceitaOrigemId
        ON dbo.Receita (ReceitaOrigemId);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 28) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_UsuarioCadastroId_Status_ReceitaOrigemId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_UsuarioCadastroId_Status_ReceitaOrigemId
        ON dbo.Receita (UsuarioCadastroId, Status, ReceitaOrigemId)
        INCLUDE (DataLancamento);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 29) */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Status' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita DROP CONSTRAINT CK_Receita_Status;
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 30) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Status' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_Status
        CHECK (Status IN (N'Pendente', N'Efetivada', N'Cancelada', N'PendenteAprovacao', N'Rejeitado'));
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 5) */
IF COL_LENGTH(N'dbo.Receita', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD ContaDestinoId BIGINT NULL;
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 7) */
IF COL_LENGTH(N'dbo.Receita', N'DespesaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD DespesaTransferenciaId BIGINT NULL;
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 8) */
IF COL_LENGTH(N'dbo.Receita', N'CartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD CartaoId BIGINT NULL;
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 11) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 13) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_ContaBancaria_ContaDestinoId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_ContaBancaria_ContaDestinoId
        FOREIGN KEY (ContaDestinoId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 15) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Despesa_DespesaTransferenciaId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Despesa_DespesaTransferenciaId
        FOREIGN KEY (DespesaTransferenciaId) REFERENCES dbo.Despesa (Id);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 19) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_CartaoId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_CartaoId ON dbo.Receita (CartaoId);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 20) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ContaDestinoId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_ContaDestinoId ON dbo.Receita (ContaDestinoId);
END;
GO

/* Origem: 15-vinculo-meio-financeiro.sql (bloco 22) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_DespesaTransferenciaId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_DespesaTransferenciaId ON dbo.Receita (DespesaTransferenciaId);
END;
GO

/* Origem: 18-fix-conta-destino-despesa-receita.sql (bloco 4) */
IF COL_LENGTH(N'dbo.Receita', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD ContaDestinoId BIGINT NULL;
END;
GO

/* Origem: 18-fix-conta-destino-despesa-receita.sql (bloco 6) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_ContaBancaria_ContaDestinoId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_ContaBancaria_ContaDestinoId
        FOREIGN KEY (ContaDestinoId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

/* Origem: 18-fix-conta-destino-despesa-receita.sql (bloco 8) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ContaDestinoId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_ContaDestinoId ON dbo.Receita (ContaDestinoId);
END;
GO

/* Origem: 19-fix-vinculo-transacao-entre-contas.sql (bloco 3) */
IF COL_LENGTH(N'dbo.Receita', N'DespesaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD DespesaTransferenciaId BIGINT NULL;
END;
GO

/* Origem: 19-fix-vinculo-transacao-entre-contas.sql (bloco 5) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Despesa_DespesaTransferenciaId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Despesa_DespesaTransferenciaId
        FOREIGN KEY (DespesaTransferenciaId) REFERENCES dbo.Despesa (Id);
END;
GO

/* Origem: 19-fix-vinculo-transacao-entre-contas.sql (bloco 7) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_DespesaTransferenciaId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_DespesaTransferenciaId ON dbo.Receita (DespesaTransferenciaId);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 5) */
IF COL_LENGTH(N'dbo.Receita', N'FaturaCartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD FaturaCartaoId BIGINT NULL;
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 11) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_FaturaCartao_FaturaCartaoId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_FaturaCartao_FaturaCartaoId
        FOREIGN KEY (FaturaCartaoId) REFERENCES dbo.FaturaCartao (Id);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 17) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_FaturaCartaoId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_FaturaCartaoId
        ON dbo.Receita (FaturaCartaoId);
END;
GO

/* Origem: 21-fix-receita-recorrencia-origem.sql (bloco 2) */
IF OBJECT_ID(N'dbo.Receita', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Receita', N'ReceitaRecorrenciaOrigemId') IS NULL
    BEGIN
        ALTER TABLE dbo.Receita ADD ReceitaRecorrenciaOrigemId BIGINT NULL;
    END
END;
GO

/* Origem: 21-fix-receita-recorrencia-origem.sql (bloco 4) */
IF OBJECT_ID(N'dbo.Receita', N'U') IS NOT NULL
BEGIN
    ;WITH SerieLegada AS
    (
        SELECT
            r.Id,
            BaseId = FIRST_VALUE(r.Id) OVER
            (
                PARTITION BY
                    r.UsuarioCadastroId,
                    r.Descricao,
                    r.TipoReceita,
                    r.TipoRecebimento,
                    r.Recorrencia,
                    r.RecorrenciaFixa,
                    r.ContaBancariaId,
                    r.CartaoId
                ORDER BY r.DataLancamento, r.Id
            )
        FROM dbo.Receita r
        WHERE r.ReceitaOrigemId IS NULL
          AND r.Recorrencia <> N'Unica'
          AND r.ReceitaRecorrenciaOrigemId IS NULL
    )
    UPDATE r
    SET r.ReceitaRecorrenciaOrigemId = s.BaseId
    FROM dbo.Receita r
    INNER JOIN SerieLegada s ON s.Id = r.Id
    WHERE r.ReceitaRecorrenciaOrigemId IS NULL;
END;
GO

/* Origem: 21-fix-receita-recorrencia-origem.sql (bloco 5) */
IF OBJECT_ID(N'dbo.Receita', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Receita_ReceitaRecorrenciaOrigemId')
    BEGIN
        ALTER TABLE dbo.Receita
            WITH CHECK ADD CONSTRAINT FK_Receita_Receita_ReceitaRecorrenciaOrigemId
            FOREIGN KEY (ReceitaRecorrenciaOrigemId) REFERENCES dbo.Receita (Id);
    END
END;
GO

/* Origem: 21-fix-receita-recorrencia-origem.sql (bloco 6) */
IF OBJECT_ID(N'dbo.Receita', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ReceitaRecorrenciaOrigemId' AND object_id = OBJECT_ID(N'dbo.Receita'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Receita_ReceitaRecorrenciaOrigemId
            ON dbo.Receita (ReceitaRecorrenciaOrigemId);
    END
END;
GO


