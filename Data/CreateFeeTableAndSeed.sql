-- Tạo bảng Fee và seed dữ liệu mặc định
USE PerfumeStore;
GO

-- Kiểm tra và tạo bảng Fee nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Fee]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Fee]
    (
        [FeeId] INT NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Value] DECIMAL(18,2) NOT NULL,
        [Description] NVARCHAR(250),
        PRIMARY KEY ([FeeId])
    );
    PRINT 'Bảng Fee đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng Fee đã tồn tại.';
END
GO

-- Xóa dữ liệu cũ nếu có (tùy chọn)
-- DELETE FROM [Fee] WHERE Name IN ('VAT', 'Shipping');
-- GO

-- Insert VAT fee (FeeId = 1)
IF NOT EXISTS (SELECT 1 FROM [Fee] WHERE Name = 'VAT')
BEGIN
    INSERT INTO [Fee] (FeeId, Name, Value, Description)
    VALUES (1, 'VAT', 10, 'Thuế giá trị gia tăng (VAT) - Áp dụng cho tất cả đơn hàng');
    PRINT 'Đã thêm VAT fee (FeeId = 1)';
END
ELSE
BEGIN
    UPDATE [Fee] 
    SET Value = 10,
        Description = 'Thuế giá trị gia tăng (VAT) - Áp dụng cho tất cả đơn hàng'
    WHERE Name = 'VAT';
    PRINT 'Đã cập nhật VAT fee';
END
GO

-- Insert Shipping fee (FeeId = 2)
IF NOT EXISTS (SELECT 1 FROM [Fee] WHERE Name = 'Shipping')
BEGIN
    INSERT INTO [Fee] (FeeId, Name, Value, Description)
    VALUES (2, 'Shipping', 30000, 'Phí vận chuyển - Chỉ áp dụng khi đơn hàng < 1,000,000 VNĐ');
    PRINT 'Đã thêm Shipping fee (FeeId = 2)';
END
ELSE
BEGIN
    UPDATE [Fee] 
    SET Value = 30000,
        Description = 'Phí vận chuyển - Chỉ áp dụng khi đơn hàng < 1,000,000 VNĐ'
    WHERE Name = 'Shipping';
    PRINT 'Đã cập nhật Shipping fee';
END
GO

-- Hiển thị kết quả
PRINT '';
PRINT '=== KẾT QUẢ ===';
SELECT * FROM [Fee] ORDER BY FeeId;
GO


