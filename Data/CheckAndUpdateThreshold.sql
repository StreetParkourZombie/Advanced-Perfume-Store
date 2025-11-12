USE PerfumeStore;
GO

-- Kiểm tra xem cột Threshold đã tồn tại chưa
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fee]') AND name = 'Threshold')
BEGIN
    PRINT 'Cột Threshold chưa tồn tại. Đang tạo cột...';
    ALTER TABLE [dbo].[Fee]
    ADD [Threshold] DECIMAL(18,2) NULL;
    PRINT 'Đã tạo cột Threshold';
END
ELSE
BEGIN
    PRINT 'Cột Threshold đã tồn tại';
END
GO

-- Kiểm tra giá trị hiện tại
PRINT 'Giá trị Threshold hiện tại:';
SELECT 
    FeeId,
    Name,
    Value,
    Threshold,
    Description
FROM [dbo].[Fee]
WHERE [Name] = 'Shipping';
GO

-- Cập nhật Threshold thành 5,000,000 VNĐ cho Shipping fee
UPDATE [dbo].[Fee]
SET [Threshold] = 5000000
WHERE [Name] = 'Shipping';
GO

-- Kiểm tra lại sau khi cập nhật
PRINT 'Giá trị Threshold sau khi cập nhật:';
SELECT 
    FeeId,
    Name,
    Value,
    Threshold,
    Description
FROM [dbo].[Fee]
WHERE [Name] = 'Shipping';
GO

PRINT 'Hoàn tất! Threshold đã được cập nhật thành 5,000,000 VNĐ';
GO

