-- VERIFICAR SE EXISTE FORING KEY
SELECT
    fk.name AS NomeFK,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) + '.' + OBJECT_NAME(fk.parent_object_id) AS TabelaFilha
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID('dbo.DespesaLog');



-- VERIFICAR COMO VAI FICAR

;WITH Mapeamento AS
(
    SELECT
        IdAtual = Id,
        NovoId  = ROW_NUMBER() OVER (ORDER BY Id)
    FROM dbo.DespesaLog
)
SELECT *
FROM Mapeamento
WHERE IdAtual <> NovoId
ORDER BY IdAtual;



-- REALIZAR O AJUSTE

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID('tempdb..#Reordenado') IS NOT NULL
        DROP TABLE #Reordenado;

    SELECT
        NovoId = ROW_NUMBER() OVER (ORDER BY Id),
        DataHoraCadastro,	UsuarioCadastroId,	DespesaId,	Acao,	Descricao
    INTO #Reordenado
    FROM dbo.DespesaLog WITH (HOLDLOCK, TABLOCKX);

    DECLARE @Qtd INT = (SELECT COUNT(*) FROM #Reordenado);

    DELETE FROM dbo.DespesaLog;

    SET IDENTITY_INSERT dbo.DespesaLog ON;

    INSERT INTO dbo.DespesaLog
    (
        Id,  DataHoraCadastro,	UsuarioCadastroId,	DespesaId,	Acao,	Descricao
    )
    SELECT
        NovoId,  DataHoraCadastro,	UsuarioCadastroId,	DespesaId,	Acao,	Descricao
    FROM #Reordenado
    ORDER BY NovoId;

    SET IDENTITY_INSERT dbo.DespesaLog OFF;

    DECLARE @cmd NVARCHAR(300);
    SET @cmd = N'DBCC CHECKIDENT (''dbo.DespesaLog'', RESEED, ' 
             + CAST(@Qtd AS VARCHAR(20)) 
             + N') WITH NO_INFOMSGS;';
    EXEC (@cmd);

    DROP TABLE #Reordenado;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK;

    IF OBJECT_ID('tempdb..#Reordenado') IS NOT NULL
        DROP TABLE #Reordenado;

    THROW;
END CATCH;