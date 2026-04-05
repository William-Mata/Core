DECLARE @SchemaName SYSNAME = N'dbo';
DECLARE @TableName SYSNAME = N'Despesa';

DECLARE @FullName NVARCHAR(514) = QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);

IF OBJECT_ID(@FullName, N'U') IS NULL
BEGIN
    RAISERROR('Tabela %s não existe.', 16, 1, @FullName);
    RETURN;
END;

DECLARE @sql NVARCHAR(MAX);

BEGIN TRANSACTION;
    SET @sql = N'DELETE FROM ' + @FullName + N';';
    EXEC sp_executesql @sql;

    SET @sql = N'DBCC CHECKIDENT(''' + @FullName + ''', RESEED, 0);';
    EXEC sp_executesql @sql;
COMMIT TRANSACTION;