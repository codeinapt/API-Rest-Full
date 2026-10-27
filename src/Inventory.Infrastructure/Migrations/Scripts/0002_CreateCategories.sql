IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id INT IDENTITY(1, 1) NOT NULL,
        AreaId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
        CreatedAt DATETIME2(3) NOT NULL,
        UpdatedAt DATETIME2(3) NULL,
        CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Categories_Name' AND object_id = OBJECT_ID(N'dbo.Categories'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Categories_Name ON dbo.Categories (Name) WHERE IsActive = 1;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_AreaId_IsActive' AND object_id = OBJECT_ID(N'dbo.Categories'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Categories_AreaId_IsActive ON dbo.Categories (AreaId, IsActive);
END;
