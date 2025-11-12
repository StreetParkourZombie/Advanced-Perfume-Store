 USE PerfumeStore;
GO

-- Cập nhật ngưỡng phí vận chuyển thành 5,000,000 VNĐ
UPDATE [dbo].[Fee]
SET [Threshold] = 5000000
WHERE [Name] = 'Shipping';
GO

-- Hiển thị kết quả
SELECT 
    FeeId,
    Name,
    Value,
    Threshold,
    Description
FROM [dbo].[Fee]
WHERE [Name] = 'Shipping';
GO

PRINT 'Đã cập nhật ngưỡng phí vận chuyển thành 5,000,000 VNĐ';
GO

