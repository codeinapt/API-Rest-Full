IF NOT EXISTS (SELECT 1 FROM dbo.Areas)
BEGIN
    DECLARE @SeededAt DATETIME2(3) = SYSUTCDATETIME();
    INSERT INTO dbo.Areas (Code, Name, IsActive, CreatedAt)
    VALUES
        ('FOOD', N'Alimentos', 1, @SeededAt),
        ('CLEAN', N'Aseo', 1, @SeededAt),
        ('TOOLS', N'Herramientas', 1, @SeededAt);
    INSERT INTO dbo.Categories (AreaId, Name, IsActive, CreatedAt)
    SELECT a.Id, seed.Name, 1, @SeededAt
    FROM (VALUES
        ('FOOD', N'Lácteos'),
        ('FOOD', N'Despensa'),
        ('CLEAN', N'Limpieza del hogar'),
        ('CLEAN', N'Higiene personal'),
        ('TOOLS', N'Herramientas manuales'),
        ('TOOLS', N'Ferretería')) AS seed (AreaCode, Name)
    INNER JOIN dbo.Areas a ON a.Code = seed.AreaCode AND a.IsActive = 1;
    INSERT INTO dbo.Products (Sku, Name, Description, CategoryId, Stock, MinimumStock, IsActive, CreatedAt)
    SELECT seed.Sku, seed.Name, seed.Description, c.Id, 0, seed.MinimumStock, 1, @SeededAt
    FROM (VALUES
        ('FOOD-MILK-001', N'Leche entera 1 L', N'Brik de leche entera de vaca.', N'Lácteos', 20),
        ('FOOD-YOG-001', N'Yogur natural pack 4', N'Pack de cuatro yogures naturales.', N'Lácteos', 10),
        ('FOOD-RICE-001', N'Arroz largo 1 kg', NULL, N'Despensa', 15),
        ('FOOD-OIL-001', N'Aceite de oliva 1 L', N'Aceite de oliva virgen extra.', N'Despensa', 10),
        ('CLEAN-DET-001', N'Detergente líquido 3 L', NULL, N'Limpieza del hogar', 10),
        ('CLEAN-BLE-001', N'Lejía 2 L', NULL, N'Limpieza del hogar', 10),
        ('CLEAN-SOAP-001', N'Jabón de manos 500 ml', NULL, N'Higiene personal', 15),
        ('CLEAN-SHAM-001', N'Champú 500 ml', NULL, N'Higiene personal', 5),
        ('TOOLS-HAM-001', N'Martillo de carpintero', N'Martillo de 500 g con mango de fibra.', N'Herramientas manuales', 5),
        ('TOOLS-SCR-001', N'Juego de destornilladores', N'Seis piezas, punta plana y de estrella.', N'Herramientas manuales', 5),
        ('TOOLS-SCRW-001', N'Tornillos 4x40 caja 100', NULL, N'Ferretería', 20),
        ('TOOLS-NAIL-001', N'Clavos 50 mm caja', NULL, N'Ferretería', 20)) AS seed (Sku, Name, Description, CategoryName, MinimumStock)
    INNER JOIN dbo.Categories c ON c.Name = seed.CategoryName AND c.IsActive = 1;
    DECLARE @InitialStock TABLE (Sku VARCHAR(50) NOT NULL, Quantity INT NOT NULL);
    INSERT INTO @InitialStock (Sku, Quantity)
    VALUES
        ('FOOD-MILK-001', 50),
        ('FOOD-YOG-001', 5),
        ('FOOD-RICE-001', 40),
        ('CLEAN-DET-001', 25),
        ('CLEAN-BLE-001', 8),
        ('CLEAN-SOAP-001', 30),
        ('CLEAN-SHAM-001', 12),
        ('TOOLS-HAM-001', 15),
        ('TOOLS-SCR-001', 3),
        ('TOOLS-SCRW-001', 100),
        ('TOOLS-NAIL-001', 60);
    INSERT INTO dbo.InventoryMovements (ProductId, Type, Delta, PreviousStock, ResultingStock, Notes, CreatedAt, CreatedBy)
    SELECT p.Id, 1, s.Quantity, p.Stock, p.Stock + s.Quantity, N'Stock inicial', @SeededAt, N'seed'
    FROM @InitialStock s
    INNER JOIN dbo.Products p ON p.Sku = s.Sku AND p.IsActive = 1;
    UPDATE p
    SET p.Stock = p.Stock + s.Quantity
    FROM dbo.Products p
    INNER JOIN @InitialStock s ON s.Sku = p.Sku
    WHERE p.IsActive = 1;
END;
