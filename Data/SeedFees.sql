-- Seed default Fees (VAT and Shipping)
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

-- Lưu ý: FeeId không có IDENTITY, cần chỉ định giá trị khi INSERT

-- Insert VAT fee if not exists
IF NOT EXISTS (SELECT 1 FROM [Fee] WHERE Name = 'VAT')
BEGIN
    -- Tìm FeeId tiếp theo
    DECLARE @VATFeeId INT;
    SELECT @VATFeeId = ISNULL(MAX(FeeId), 0) + 1 FROM [Fee];
    
    INSERT INTO [Fee] (FeeId, Name, Value, Description)
    VALUES (@VATFeeId, 'VAT', 10, 'Thuế giá trị gia tăng (VAT) - Áp dụng cho tất cả đơn hàng');
    PRINT 'Đã thêm VAT fee';
END
ELSE
BEGIN
    UPDATE [Fee] 
    SET Description = 'Thuế giá trị gia tăng (VAT) - Áp dụng cho tất cả đơn hàng'
    WHERE Name = 'VAT';
    PRINT 'Đã cập nhật VAT fee';
END
GO

-- Insert Shipping fee if not exists
IF NOT EXISTS (SELECT 1 FROM [Fee] WHERE Name = 'Shipping')
BEGIN
    -- Tìm FeeId tiếp theo
    DECLARE @ShippingFeeId INT;
    SELECT @ShippingFeeId = ISNULL(MAX(FeeId), 0) + 1 FROM [Fee];
    
    INSERT INTO [Fee] (FeeId, Name, Value, Description)
    VALUES (@ShippingFeeId, 'Shipping', 30000, 'Phí vận chuyển - Chỉ áp dụng khi đơn hàng < 1,000,000 VNĐ');
    PRINT 'Đã thêm Shipping fee';
END
ELSE
BEGIN
    UPDATE [Fee] 
    SET Description = 'Phí vận chuyển - Chỉ áp dụng khi đơn hàng < 1,000,000 VNĐ'
    WHERE Name = 'Shipping';
    PRINT 'Đã cập nhật Shipping fee';
END
GO

-- Verify
SELECT * FROM [Fee];
GO
