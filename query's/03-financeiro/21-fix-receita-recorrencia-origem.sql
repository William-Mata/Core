/*
Hotfix: ancora de recorrencia para Receita
- adiciona ReceitaRecorrenciaOrigemId
- cria FK/indice
- preenche legado com id da transacao base da serie
*/

USE [Financeiro];
GO

IF OBJECT_ID(N'dbo.Receita', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Receita', N'ReceitaRecorrenciaOrigemId') IS NULL
    BEGIN
        ALTER TABLE dbo.Receita ADD ReceitaRecorrenciaOrigemId BIGINT NULL;
    END
END;
GO

/*
Validacao previa (SELECT) antes do UPDATE:
- mostra amostra das series legadas sem ancora
*/
IF OBJECT_ID(N'dbo.Receita', N'U') IS NOT NULL
BEGIN
    ;WITH SerieLegada AS
    (
        SELECT
            r.Id,
            r.UsuarioCadastroId,
            r.Descricao,
            r.TipoReceita,
            r.TipoRecebimento,
            r.Recorrencia,
            r.RecorrenciaFixa,
            r.ContaBancariaId,
            r.CartaoId,
            r.DataLancamento,
            r.DataVencimento,
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
    SELECT TOP (200)
        Id,
        BaseId,
        UsuarioCadastroId,
        Descricao,
        TipoReceita,
        TipoRecebimento,
        Recorrencia,
        RecorrenciaFixa,
        ContaBancariaId,
        CartaoId,
        DataLancamento,
        DataVencimento
    FROM SerieLegada
    ORDER BY UsuarioCadastroId, BaseId, DataLancamento, Id;
END;
GO

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

IF OBJECT_ID(N'dbo.Receita', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ReceitaRecorrenciaOrigemId' AND object_id = OBJECT_ID(N'dbo.Receita'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Receita_ReceitaRecorrenciaOrigemId
            ON dbo.Receita (ReceitaRecorrenciaOrigemId);
    END
END;
GO
