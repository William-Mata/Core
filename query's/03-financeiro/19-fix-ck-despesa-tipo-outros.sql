/*
Ordem sugerida: 19
Objetivo: permitir tipo de despesa "outros" na constraint CK_Despesa_TipoDespesa.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

SELECT
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Despesa')
  AND cc.name = N'CK_Despesa_TipoDespesa';
GO

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
        CHECK (TipoDespesa IN (N'alimentacao', N'transporte', N'moradia', N'lazer', N'saude', N'educacao', N'servicos', N'outros'));
END;
GO

SELECT
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Despesa')
  AND cc.name = N'CK_Despesa_TipoDespesa';
GO
