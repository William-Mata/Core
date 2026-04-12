-- VERIFICAR SE EXISTE FORING KEY
SELECT
    fk.name AS NomeFK,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) + '.' + OBJECT_NAME(fk.parent_object_id) AS TabelaFilha
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID('dbo.HistoricoTransacaoFinanceira');



-- VERIFICAR COMO VAI FICAR

;WITH Mapeamento AS
(
    SELECT
        IdAtual = Id,
        NovoId  = ROW_NUMBER() OVER (ORDER BY Id)
    FROM dbo.HistoricoTransacaoFinanceira
)
SELECT *
FROM Mapeamento
WHERE IdAtual <> NovoId
ORDER BY IdAtual;


select * from HistoricoTransacaoFinanceira where id = 0


-- REALIZAR O AJUSTE

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID('tempdb..#Reordenado') IS NOT NULL
        DROP TABLE #Reordenado;

    SELECT
        NovoId = ROW_NUMBER() OVER (ORDER BY Id),
        DataHoraCadastro, UsuarioOperacaoId,	TipoTransacao,	TransacaoId,	TipoOperacao,	TipoConta,	ContaBancariaId,	CartaoId,	DataTransacao,	Descricao,	TipoPagamento,	ValorAntesTransacao,	ValorTransacao,	ValorDepoisTransacao,	TipoRecebimento,	Observacao,	OcultarDoHistorico,	ContaDestinoId
    INTO #Reordenado
    FROM dbo.HistoricoTransacaoFinanceira WITH (HOLDLOCK, TABLOCKX);

    DECLARE @Qtd INT = (SELECT COUNT(*) FROM #Reordenado);

    DELETE FROM dbo.HistoricoTransacaoFinanceira;

    SET IDENTITY_INSERT dbo.HistoricoTransacaoFinanceira ON;

    INSERT INTO dbo.HistoricoTransacaoFinanceira
    (
        Id,	         DataHoraCadastro, UsuarioOperacaoId,	TipoTransacao,	TransacaoId,	TipoOperacao,	TipoConta,	ContaBancariaId,	CartaoId,	DataTransacao,	Descricao,	TipoPagamento,	ValorAntesTransacao,	ValorTransacao,	ValorDepoisTransacao,	TipoRecebimento,	Observacao,	OcultarDoHistorico,	ContaDestinoId


    )
    SELECT
        NovoId,           DataHoraCadastro, UsuarioOperacaoId,	TipoTransacao,	TransacaoId,	TipoOperacao,	TipoConta,	ContaBancariaId,	CartaoId,	DataTransacao,	Descricao,	TipoPagamento,	ValorAntesTransacao,	ValorTransacao,	ValorDepoisTransacao,	TipoRecebimento,	Observacao,	OcultarDoHistorico,	ContaDestinoId


    FROM #Reordenado
    ORDER BY NovoId;

    SET IDENTITY_INSERT dbo.HistoricoTransacaoFinanceira OFF;

    DECLARE @cmd NVARCHAR(300);
    SET @cmd = N'DBCC CHECKIDENT (''dbo.HistoricoTransacaoFinanceira'', RESEED, ' 
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



--DELETE HistoricoTransacaoFinanceira

--DBCC CHECKIDENT (HistoricoTransacaoFinanceira, RESEED, 1 )