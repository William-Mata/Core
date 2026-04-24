/*
Utilitário: garantir usuário admin (Id = 1) com todas as permissões ativas.
Script idempotente (pode ser executado mais de uma vez).
*/

USE [Financeiro];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Usuario ON;

    INSERT INTO dbo.Usuario
    (
        Id,
        DataHoraCadastro,
        UsuarioCadastroId,
        DataNascimento,
        Nome,
        Email,
        SenhaHash,
        Ativo,
        PrimeiroAcesso,
        PerfilId
    )
    VALUES
    (
        1,
        '2026-01-01T00:00:00',
        1,
        NULL,
        N'Usuário Admin',
        N'admin@core.com',
        N'PBKDF2$100000$DqVvtU2jQnWQTuqbL+H8aQ==$zvCjIqD8J/r93o4azALW2k8vIjoWtM5ikW7PKfY2PA8=',
        1,
        1,
        1
    );

    SET IDENTITY_INSERT dbo.Usuario OFF;
END;
GO

INSERT INTO dbo.UsuarioModulo (DataHoraCadastro, UsuarioCadastroId, UsuarioId, ModuloId, Status)
SELECT SYSUTCDATETIME(), 1, 1, m.Id, 1
FROM dbo.Modulo m
WHERE m.Status = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.UsuarioModulo um
      WHERE um.UsuarioId = 1
        AND um.ModuloId = m.Id
  );
GO

INSERT INTO dbo.UsuarioTela (DataHoraCadastro, UsuarioCadastroId, UsuarioId, TelaId, Status)
SELECT SYSUTCDATETIME(), 1, 1, t.Id, 1
FROM dbo.Tela t
WHERE t.Status = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.UsuarioTela ut
      WHERE ut.UsuarioId = 1
        AND ut.TelaId = t.Id
  );
GO

INSERT INTO dbo.UsuarioFuncionalidade (DataHoraCadastro, UsuarioCadastroId, UsuarioId, FuncionalidadeId, Status)
SELECT SYSUTCDATETIME(), 1, 1, f.Id, 1
FROM dbo.Funcionalidade f
WHERE f.Status = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.UsuarioFuncionalidade uf
      WHERE uf.UsuarioId = 1
        AND uf.FuncionalidadeId = f.Id
  );
GO

SELECT
    u.Id AS UsuarioId,
    u.Email,
    u.Ativo,
    (SELECT COUNT(1) FROM dbo.UsuarioModulo um WHERE um.UsuarioId = u.Id AND um.Status = 1) AS ModulosAtivos,
    (SELECT COUNT(1) FROM dbo.UsuarioTela ut WHERE ut.UsuarioId = u.Id AND ut.Status = 1) AS TelasAtivas,
    (SELECT COUNT(1) FROM dbo.UsuarioFuncionalidade uf WHERE uf.UsuarioId = u.Id AND uf.Status = 1) AS FuncionalidadesAtivas
FROM dbo.Usuario u
WHERE u.Id = 1;
GO
