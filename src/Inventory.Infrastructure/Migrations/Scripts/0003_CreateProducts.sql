IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id INT IDENTITY(1, 1) NOT NULL,
        Sku VARCHAR(50) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(1000) NULL,
        CategoryId INT NOT NULL,
        Stock INT NOT NULL CONSTRAINT DF_Products_Stock DEFAULT (0),
        MinimumStock INT NOT NULL CONSTRAINT DF_Products_MinimumStock DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        DeactivationReason TINYINT NULL,
        DeactivationNotes NVARCHAR(500) NULL,
        DeactivatedAt DATETIME2(3) NULL,
        DeactivatedBy NVARCHAR(100) NULL,
        ReactivatedAt DATETIME2(3) NULL,
        ReactivatedBy NVARCHAR(100) NULL,
        CreatedAt DATETIME2(3) NOT NULL,
        UpdatedAt DATETIME2(3) NULL,
        CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Products_Stock_NotNegative CHECK (Stock >= 0),
        CONSTRAINT CK_Products_MinimumStock_NotNegative CHECK (MinimumStock >= 0)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Products_Sku' AND object_id = OBJECT_ID(N'dbo.Products'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Products_Sku ON dbo.Products (Sku) WHERE IsActive = 1;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CategoryId_IsActive' AND object_id = OBJECT_ID(N'dbo.Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_CategoryId_IsActive ON dbo.Products (CategoryId, IsActive);
END;
