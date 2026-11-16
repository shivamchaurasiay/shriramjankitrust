IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'mindforg_ramjankingo')
BEGIN
    CREATE DATABASE [mindforg_ramjankingo];
END;
GO

IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'shramjankingo')
BEGIN
    CREATE LOGIN [shramjankingo] WITH PASSWORD = 'IAcA4je@yl8v@s1b', CHECK_POLICY = OFF;
END
ELSE
BEGIN
    ALTER LOGIN [shramjankingo] WITH PASSWORD = 'IAcA4je@yl8v@s1b';
END;
GO

USE [mindforg_ramjankingo];
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'shramjankingo')
BEGIN
    CREATE USER [shramjankingo] FOR LOGIN [shramjankingo];
    ALTER ROLE db_owner ADD MEMBER [shramjankingo];
END;
GO
