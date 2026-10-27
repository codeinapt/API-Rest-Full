IF OBJECT_ID(N'dbo.InventoryMovements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryMovements
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL,
        ProductId INT NOT NULL,
        Type TINYINT NOT NULL,
        Delta INT NOT NULL,
        PreviousStock INT NOT NULL,
        ResultingStock INT NOT NULL,
        AdjustmentReason TINYINT NULL,
        Notes NVARCHAR(500) NULL,
        RelatedMovementId BIGINT NULL,
        CreatedAt DATETIME2(3) NOT NULL,
        CreatedBy NVARCHAR(100) NOT NULL,
        CONSTRAINT PK_InventoryMovements PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_InventoryMovements_Delta_NotZero CHECK (Delta <> 0),
        CONSTRAINT CK_InventoryMovements_PreviousStock_NotNegative CHECK (PreviousStock >= 0),
        CONSTRAINT CK_InventoryMovements_ResultingStock_NotNegative CHECK (ResultingStock >= 0),
        CONSTRAINT CK_InventoryMovements_ResultingStock_MatchesDelta CHECK (ResultingStock = PreviousStock + Delta),
        CONSTRAINT CK_InventoryMovements_AdjustmentReason_RequiredForAdjustment CHECK (Type <> 3 OR AdjustmentReason IS NOT NULL)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InventoryMovements_ProductId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.InventoryMovements'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventoryMovements_ProductId_CreatedAt ON dbo.InventoryMovements (ProductId, CreatedAt DESC);
END;
