-- Seed data for Comments table
-- This script will insert sample comments for testing the comment system

-- First, let's update the Rating column from bit to int if needed
-- (This might be needed if the database still has the old schema)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Comments' AND COLUMN_NAME = 'Rating' AND DATA_TYPE = 'bit')
BEGIN
    ALTER TABLE Comments ALTER COLUMN Rating int;
END

-- Insert sample customers if they don't exist
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerID = 1)
BEGIN
    INSERT INTO Customers (CustomerID, Name, Phone, Email, BirthYear, CreatedDate, PasswordHash, SpinNumber, MembershipID)
    VALUES 
    (1, 'Nguyễn Văn An', '0123456789', 'an.nguyen@email.com', 1990, GETDATE(), 'hashed_password', 5, NULL),
    (2, 'Trần Thị Bình', '0987654321', 'binh.tran@email.com', 1985, GETDATE(), 'hashed_password', 3, NULL),
    (3, 'Lê Văn Cường', '0369852147', 'cuong.le@email.com', 1992, GETDATE(), 'hashed_password', 7, NULL),
    (4, 'Phạm Thị Dung', '0147258369', 'dung.pham@email.com', 1988, GETDATE(), 'hashed_password', 2, NULL),
    (5, 'Hoàng Văn Em', '0258369741', 'em.hoang@email.com', 1995, GETDATE(), 'hashed_password', 4, NULL);
END

-- Insert sample products if they don't exist
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductID = 1)
BEGIN
    INSERT INTO Products (ProductID, ProductName, Description, Price, BrandID, IsPublished, CreatedDate)
    VALUES 
    (1, 'Chanel Bleu De Chanel EDP', 'Hương thơm nam tính và tinh tế', 4080000, 1, 1, GETDATE()),
    (2, 'Yves Saint Laurent Libre EDP', 'Hương thơm sang trọng và nhẹ nhàng', 3200000, 2, 1, GETDATE());
END

-- Insert sample comments
-- Delete existing comments for product 1 to avoid duplicates
DELETE FROM Comments WHERE ProductID = 1;

-- Insert new sample comments
INSERT INTO Comments (ProductID, CustomerID, CommentDate, Rating, Content)
VALUES 
    -- Comments for Chanel Bleu De Chanel EDP (ProductID = 1)
    (1, 1, '2024-01-15 10:30:00', 5, 'Sản phẩm tuyệt vời! Mùi hương rất nam tính và lịch lãm. Tôi rất hài lòng với chất lượng.'),
    (1, 2, '2024-01-20 14:15:00', 4, 'Mùi hương khá tốt, độ bền cũng ổn. Giá hơi cao nhưng đáng tiền.'),
    (1, 3, '2024-02-01 09:45:00', 5, 'Đã sử dụng được 2 tháng, mùi hương vẫn rất thơm. Sẽ mua lại lần nữa.'),
    (1, 4, '2024-02-10 16:20:00', 3, 'Tạm được, không quá ấn tượng như mong đợi.'),
    (1, 5, '2024-02-15 11:30:00', 4, 'Chất lượng tốt, giao hàng nhanh. Khuyến nghị cho những ai yêu thích hương gỗ.');

-- Insert comments for YSL Libre EDP (ProductID = 2) if product exists
IF EXISTS (SELECT 1 FROM Products WHERE ProductID = 2)
BEGIN
    DELETE FROM Comments WHERE ProductID = 2;
    
    INSERT INTO Comments (ProductID, CustomerID, CommentDate, Rating, Content)
    VALUES 
        (2, 1, '2024-01-18 13:45:00', 5, 'Mình trước giờ dùng Chanel thôi, sau thấy YSL ra chai này mình ngửi thử thôi mà mê luôn, mùi sang trọng mà nhẹ nhàng nên mình dùng đi làm gặp đối tác cực kỳ thích hợp'),
        (2, 2, '2024-01-25 15:30:00', 4, 'Tạm được. 7 điểm'),
        (2, 3, '2024-02-05 12:15:00', 5, 'Rất thích mùi hương này, phù hợp cho công việc văn phòng.'),
        (2, 4, '2024-02-12 17:00:00', 4, 'Chất lượng tốt, đóng gói cẩn thận.'),
        (2, 5, '2024-02-18 10:45:00', 3, 'Không tệ nhưng cũng không quá đặc biệt.');
END

-- Display the inserted comments
SELECT 
    c.ProductID,
    p.ProductName,
    c.CustomerID,
    cu.Name as CustomerName,
    c.Rating,
    c.Content,
    c.CommentDate
FROM Comments c
INNER JOIN Products p ON c.ProductID = p.ProductID
INNER JOIN Customers cu ON c.CustomerID = cu.CustomerID
ORDER BY c.ProductID, c.CommentDate DESC;
