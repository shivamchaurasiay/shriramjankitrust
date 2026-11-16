USE [mindforg_ramjankingo];
GO

-- 1. Set default schema to dbo for user shramjankingo
BEGIN TRY
    ALTER USER [shramjankingo] WITH DEFAULT_SCHEMA = dbo;
END TRY
BEGIN CATCH
    PRINT ERROR_MESSAGE();
END CATCH
GO

-- 2. Transfer all tables from shramjankingo schema to dbo schema
DECLARE @sqlTable NVARCHAR(MAX) = N'';
SELECT @sqlTable += N'ALTER SCHEMA dbo TRANSFER ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + CHAR(13)
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'shramjankingo';

IF LEN(@sqlTable) > 0
BEGIN
    PRINT 'Transferring tables:';
    PRINT @sqlTable;
    EXEC sp_executesql @sqlTable;
END
GO

-- 3. Transfer all procedures from shramjankingo schema to dbo schema
DECLARE @sqlProc NVARCHAR(MAX) = N'';
SELECT @sqlProc += N'ALTER SCHEMA dbo TRANSFER ' + QUOTENAME(s.name) + N'.' + QUOTENAME(p.name) + N';' + CHAR(13)
FROM sys.procedures p
JOIN sys.schemas s ON p.schema_id = s.schema_id
WHERE s.name = 'shramjankingo';

IF LEN(@sqlProc) > 0
BEGIN
    PRINT 'Transferring procedures:';
    PRINT @sqlProc;
    EXEC sp_executesql @sqlProc;
END
GO

-- 4. Transfer any views if any
DECLARE @sqlView NVARCHAR(MAX) = N'';
SELECT @sqlView += N'ALTER SCHEMA dbo TRANSFER ' + QUOTENAME(s.name) + N'.' + QUOTENAME(v.name) + N';' + CHAR(13)
FROM sys.views v
JOIN sys.schemas s ON v.schema_id = s.schema_id
WHERE s.name = 'shramjankingo';

IF LEN(@sqlView) > 0
BEGIN
    EXEC sp_executesql @sqlView;
END
GO

-- 5. Drop empty shramjankingo schema if needed
IF EXISTS (SELECT * FROM sys.schemas WHERE name = 'shramjankingo')
BEGIN
    BEGIN TRY
        DROP SCHEMA [shramjankingo];
    END TRY
    BEGIN CATCH
        PRINT 'Schema shramjankingo retained or user owned';
    END CATCH
END
GO
