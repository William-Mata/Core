/*
Ordem sugerida: 15
Objetivo: adicionar vinculo opcional de conta/cartao em despesa e receita.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

IF COL_LENGTH(N'dbo.Despesa', N'ContaBancariaId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ContaBancariaId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Despesa', N'CartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD CartaoId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Despesa', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ContaDestinoId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD ContaDestinoId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Despesa', N'ReceitaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ReceitaTransferenciaId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'DespesaTransferenciaId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD DespesaTransferenciaId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'CartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD CartaoId BIGINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_ContaBancaria_ContaBancariaId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_ContaBancaria_ContaBancariaId
        FOREIGN KEY (ContaBancariaId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_ContaBancaria_ContaDestinoId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_ContaBancaria_ContaDestinoId
        FOREIGN KEY (ContaDestinoId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_ContaBancaria_ContaDestinoId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_ContaBancaria_ContaDestinoId
        FOREIGN KEY (ContaDestinoId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Receita_ReceitaTransferenciaId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Receita_ReceitaTransferenciaId
        FOREIGN KEY (ReceitaTransferenciaId) REFERENCES dbo.Receita (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Despesa_DespesaTransferenciaId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Despesa_DespesaTransferenciaId
        FOREIGN KEY (DespesaTransferenciaId) REFERENCES dbo.Despesa (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ContaBancariaId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ContaBancariaId ON dbo.Despesa (ContaBancariaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_CartaoId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_CartaoId ON dbo.Despesa (CartaoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ContaDestinoId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ContaDestinoId ON dbo.Despesa (ContaDestinoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_CartaoId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_CartaoId ON dbo.Receita (CartaoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ContaDestinoId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_ContaDestinoId ON dbo.Receita (ContaDestinoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ReceitaTransferenciaId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ReceitaTransferenciaId ON dbo.Despesa (ReceitaTransferenciaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_DespesaTransferenciaId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_DespesaTransferenciaId ON dbo.Receita (DespesaTransferenciaId);
END;
GO
