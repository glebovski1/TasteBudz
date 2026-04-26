SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.RestaurantSlots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.RestaurantSlots', N'DiscountPercent') IS NULL
BEGIN
    ALTER TABLE dbo.RestaurantSlots
        ADD DiscountPercent INT NULL;
END;
GO

IF OBJECT_ID(N'dbo.RestaurantSlots', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.RestaurantSlots
    SET DiscountPercent = 15
    WHERE MinThresholdForDiscount IS NOT NULL
      AND DiscountPercent IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.RestaurantSlots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RestaurantSlots_DiscountPercent' AND parent_object_id = OBJECT_ID(N'dbo.RestaurantSlots'))
BEGIN
    ALTER TABLE dbo.RestaurantSlots WITH CHECK
        ADD CONSTRAINT CK_RestaurantSlots_DiscountPercent
        CHECK (DiscountPercent IS NULL OR DiscountPercent BETWEEN 1 AND 100);
END;
GO

IF OBJECT_ID(N'dbo.RestaurantSlots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RestaurantSlots_DiscountPair' AND parent_object_id = OBJECT_ID(N'dbo.RestaurantSlots'))
BEGIN
    ALTER TABLE dbo.RestaurantSlots WITH CHECK
        ADD CONSTRAINT CK_RestaurantSlots_DiscountPair
        CHECK (
            (MinThresholdForDiscount IS NULL AND DiscountPercent IS NULL) OR
            (MinThresholdForDiscount IS NOT NULL AND DiscountPercent IS NOT NULL)
        );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'20260426-restaurant-slot-discount-percent')
BEGIN
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'20260426-restaurant-slot-discount-percent', N'Add per-slot discount percentage for restaurant slot discount simulation.');
END;
GO
