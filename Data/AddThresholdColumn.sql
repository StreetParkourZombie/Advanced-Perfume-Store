USE PerfumeStore;
GO

-- Thêm cột Threshold vào bảng Fee
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fee]') AND name = 'Threshold')
BEGIN
    ALTER TABLE [dbo].[Fee]
    ADD [Threshold] DECIMAL(18,2) NULL;
    
    PRINT 'Đã thêm cột Threshold vào bảng Fee';
END
ELSE
BEGIN
    PRINT 'Cột Threshold đã tồn tại trong bảng Fee';
END
GO

-- Cập nhật Threshold mặc định cho Shipping fee (5,000,000 VNĐ)
UPDATE [dbo].[Fee]
SET [Threshold] = 5000000
WHERE [Name] = 'Shipping' AND [Threshold] IS NULL;
GO

-- Hiển thị kết quả
SELECT 
    FeeId,
    Name,
    Value,
    Threshold,
    Description
FROM [dbo].[Fee];
GO

