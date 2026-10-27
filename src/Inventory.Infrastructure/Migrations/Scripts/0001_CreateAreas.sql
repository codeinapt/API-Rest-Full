IF OBJECT_ID(N'dbo.Areas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Areas
    (
        Id INT IDENTITY(1, 1) NOT NULL,
        Code VARCHAR(10) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Areas_IsActive DEFAULT (1),
        CreatedAt DATETIME2(3) NOT NULL,
        UpdatedAt DATETIME2(3) NULL,
        CONSTRAINT PK_Areas PRIMARY KEY CLUSTERED (Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Areas_Code' AND object_id = OBJECT_ID(N'dbo.Areas'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Areas_Code ON dbo.Areas (Code) WHERE IsActive = 1;
END;
