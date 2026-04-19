/*
Ordem sugerida: 22
Objetivo: normalizar colunas de data/hora para DATETIME2(0) nas tabelas financeiras,
          tratando indices dependentes durante o ALTER COLUMN.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

IF OBJECT_ID('tempdb..#ColunasAlvo') IS NOT NULL DROP TABLE #ColunasAlvo;
CREATE TABLE #ColunasAlvo
(
    SchemaName SYSNAME NOT NULL,
    TableName SYSNAME NOT NULL,
    ColumnName SYSNAME NOT NULL
);

INSERT INTO #ColunasAlvo (SchemaName, TableName, ColumnName)
VALUES
    (N'dbo', N'Despesa', N'DataLancamento'),
    (N'dbo', N'Despesa', N'DataEfetivacao'),
    (N'dbo', N'Receita', N'DataLancamento'),
    (N'dbo', N'Receita', N'DataEfetivacao'),
    (N'dbo', N'Reembolso', N'DataLancamento'),
    (N'dbo', N'Reembolso', N'DataEfetivacao');

PRINT 'PRE-VALIDACAO: tipos atuais das colunas alvo';
SELECT
    ca.SchemaName AS SchemaName,
    ca.TableName AS TableName,
    ca.ColumnName AS ColumnName,
    ty.name AS TipoAtual,
    c.max_length AS MaxLengthBytes,
    c.scale AS Escala,
    c.is_nullable AS AceitaNulo
FROM #ColunasAlvo ca
LEFT JOIN sys.tables t
    ON t.name = ca.TableName
   AND t.schema_id = SCHEMA_ID(ca.SchemaName)
LEFT JOIN sys.columns c
    ON c.object_id = t.object_id
   AND c.name = ca.ColumnName
LEFT JOIN sys.types ty
    ON ty.user_type_id = c.user_type_id
ORDER BY ca.TableName, ca.ColumnName;
GO

IF OBJECT_ID('tempdb..#IndicesAfetados') IS NOT NULL DROP TABLE #IndicesAfetados;
CREATE TABLE #IndicesAfetados
(
    RowId INT IDENTITY(1,1) PRIMARY KEY,
    ObjectId INT NOT NULL,
    IndexId INT NOT NULL,
    SchemaName SYSNAME NOT NULL,
    TableName SYSNAME NOT NULL,
    IndexName SYSNAME NOT NULL,
    CreateStatement NVARCHAR(MAX) NOT NULL
);

;WITH IndicesImpactados AS
(
    SELECT DISTINCT
        t.object_id AS ObjectId,
        i.index_id AS IndexId,
        s.name AS SchemaName,
        t.name AS TableName,
        i.name AS IndexName,
        i.is_unique AS IsUnique,
        i.type_desc AS TypeDesc,
        i.filter_definition AS FilterDefinition
    FROM #ColunasAlvo ca
    INNER JOIN sys.tables t
        ON t.name = ca.TableName
       AND t.schema_id = SCHEMA_ID(ca.SchemaName)
    INNER JOIN sys.columns c
        ON c.object_id = t.object_id
       AND c.name = ca.ColumnName
    INNER JOIN sys.index_columns ic
        ON ic.object_id = c.object_id
       AND ic.column_id = c.column_id
    INNER JOIN sys.indexes i
        ON i.object_id = ic.object_id
       AND i.index_id = ic.index_id
    INNER JOIN sys.schemas s
        ON s.schema_id = t.schema_id
    WHERE i.is_primary_key = 0
      AND i.is_unique_constraint = 0
      AND i.is_hypothetical = 0
      AND i.name IS NOT NULL
)
INSERT INTO #IndicesAfetados (ObjectId, IndexId, SchemaName, TableName, IndexName, CreateStatement)
SELECT
    ii.ObjectId,
    ii.IndexId,
    ii.SchemaName,
    ii.TableName,
    ii.IndexName,
    N'CREATE '
    + CASE WHEN ii.IsUnique = 1 THEN N'UNIQUE ' ELSE N'' END
    + ii.TypeDesc + N' INDEX ' + QUOTENAME(ii.IndexName)
    + N' ON ' + QUOTENAME(ii.SchemaName) + N'.' + QUOTENAME(ii.TableName)
    + N' (' + kc.KeyColumns + N')'
    + CASE WHEN icl.IncludeColumns IS NOT NULL AND icl.IncludeColumns <> N'' THEN N' INCLUDE (' + icl.IncludeColumns + N')' ELSE N'' END
    + CASE WHEN ii.FilterDefinition IS NOT NULL AND LTRIM(RTRIM(ii.FilterDefinition)) <> N'' THEN N' WHERE ' + ii.FilterDefinition ELSE N'' END
    + N';'
FROM IndicesImpactados ii
CROSS APPLY
(
    SELECT
        STUFF
        (
            (
                SELECT
                    N', ' + QUOTENAME(c2.name) + CASE WHEN ic2.is_descending_key = 1 THEN N' DESC' ELSE N' ASC' END
                FROM sys.index_columns ic2
                INNER JOIN sys.columns c2
                    ON c2.object_id = ic2.object_id
                   AND c2.column_id = ic2.column_id
                WHERE ic2.object_id = ii.ObjectId
                  AND ic2.index_id = ii.IndexId
                  AND ic2.key_ordinal > 0
                ORDER BY ic2.key_ordinal
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'),
            1, 2, N''
        ) AS KeyColumns
) kc
CROSS APPLY
(
    SELECT
        STUFF
        (
            (
                SELECT
                    N', ' + QUOTENAME(c3.name)
                FROM sys.index_columns ic3
                INNER JOIN sys.columns c3
                    ON c3.object_id = ic3.object_id
                   AND c3.column_id = ic3.column_id
                WHERE ic3.object_id = ii.ObjectId
                  AND ic3.index_id = ii.IndexId
                  AND ic3.is_included_column = 1
                ORDER BY ic3.index_column_id
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'),
            1, 2, N''
        ) AS IncludeColumns
) icl;

DECLARE @Sql NVARCHAR(MAX);
DECLARE @SchemaName SYSNAME;
DECLARE @TableName SYSNAME;
DECLARE @ColumnName SYSNAME;
DECLARE @IsNullable BIT;

DECLARE @DropStatement NVARCHAR(MAX);
DECLARE @CreateStatement NVARCHAR(MAX);
DECLARE @IdxSchema SYSNAME;
DECLARE @IdxTable SYSNAME;
DECLARE @IdxName SYSNAME;

DECLARE cursor_drop CURSOR LOCAL FAST_FORWARD FOR
SELECT
    N'DROP INDEX ' + QUOTENAME(IndexName) + N' ON ' + QUOTENAME(SchemaName) + N'.' + QUOTENAME(TableName) + N';' AS DropStatement
FROM #IndicesAfetados
ORDER BY RowId;

OPEN cursor_drop;
FETCH NEXT FROM cursor_drop INTO @DropStatement;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT N'EXECUTANDO: ' + @DropStatement;
    EXEC sp_executesql @DropStatement;

    FETCH NEXT FROM cursor_drop INTO @DropStatement;
END;

CLOSE cursor_drop;
DEALLOCATE cursor_drop;

DECLARE cursor_colunas CURSOR LOCAL FAST_FORWARD FOR
SELECT
    ca.SchemaName,
    ca.TableName,
    ca.ColumnName,
    c.is_nullable AS IsNullable
FROM #ColunasAlvo ca
INNER JOIN sys.tables t
    ON t.name = ca.TableName
   AND t.schema_id = SCHEMA_ID(ca.SchemaName)
INNER JOIN sys.columns c
    ON c.object_id = t.object_id
   AND c.name = ca.ColumnName
INNER JOIN sys.types ty
    ON ty.user_type_id = c.user_type_id
WHERE NOT (ty.name = N'datetime2' AND c.scale = 0);

OPEN cursor_colunas;
FETCH NEXT FROM cursor_colunas INTO @SchemaName, @TableName, @ColumnName, @IsNullable;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql =
        N'ALTER TABLE ' + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName) +
        N' ALTER COLUMN ' + QUOTENAME(@ColumnName) + N' DATETIME2(0) ' +
        CASE WHEN @IsNullable = 1 THEN N'NULL;' ELSE N'NOT NULL;' END;

    PRINT N'EXECUTANDO: ' + @Sql;
    EXEC sp_executesql @Sql;

    FETCH NEXT FROM cursor_colunas INTO @SchemaName, @TableName, @ColumnName, @IsNullable;
END;

CLOSE cursor_colunas;
DEALLOCATE cursor_colunas;

DECLARE cursor_create CURSOR LOCAL FAST_FORWARD FOR
SELECT SchemaName, TableName, IndexName, CreateStatement
FROM #IndicesAfetados
ORDER BY RowId;

OPEN cursor_create;
FETCH NEXT FROM cursor_create INTO @IdxSchema, @IdxTable, @IdxName, @CreateStatement;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT N'RECRIANDO INDICE: ' + QUOTENAME(@IdxSchema) + N'.' + QUOTENAME(@IdxTable) + N'.' + QUOTENAME(@IdxName);
    EXEC sp_executesql @CreateStatement;

    FETCH NEXT FROM cursor_create INTO @IdxSchema, @IdxTable, @IdxName, @CreateStatement;
END;

CLOSE cursor_create;
DEALLOCATE cursor_create;

PRINT 'POS-VALIDACAO: tipos finais das colunas alvo';
SELECT
    ca.SchemaName AS SchemaName,
    ca.TableName AS TableName,
    ca.ColumnName AS ColumnName,
    ty.name AS TipoFinal,
    c.max_length AS MaxLengthBytes,
    c.scale AS Escala,
    c.is_nullable AS AceitaNulo
FROM #ColunasAlvo ca
LEFT JOIN sys.tables t
    ON t.name = ca.TableName
   AND t.schema_id = SCHEMA_ID(ca.SchemaName)
LEFT JOIN sys.columns c
    ON c.object_id = t.object_id
   AND c.name = ca.ColumnName
LEFT JOIN sys.types ty
    ON ty.user_type_id = c.user_type_id
ORDER BY ca.TableName, ca.ColumnName;
GO
