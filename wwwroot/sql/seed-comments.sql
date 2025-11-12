-- Simple seed data for comments
-- Run this in SQL Server Management Studio

-- Insert sample customers
INSERT INTO Customers (CustomerID, Name, Phone, Email, BirthYear, CreatedDate, PasswordHash, SpinNumber, MembershipID)
VALUES 
(1, 'Nguyễn Văn An', '0123456789', 'an.nguyen@email.com', 1990, GETDATE(), 'hashed_password', 5, NULL),
(2, 'Trần Thị Bình', '0987654321', 'binh.tran@email.com', 1985, GETDATE(), 'hashed_password', 3, NULL),
(3, 'Lê Văn Cường', '0369852147', 'cuong.le@email.com', 1992, GETDATE(), 'hashed_password', 7, NULL)
ON DUPLICATE KEY UPDATE Name = VALUES(Name);

-- Insert sample comments for product 1
DELETE FROM Comments WHERE ProductID = 1;

INSERT INTO Comments (ProductID, CustomerID, CommentDate, Rating, Content)
VALUES 
(1, 1, '2024-01-15 10:30:00', 5, 'Sản phẩm tuyệt vời! Mùi hương rất nam tính và lịch lãm. Tôi rất hài lòng với chất lượng.'),
(1, 2, '2024-01-20 14:15:00', 4, 'Mùi hương khá tốt, độ bền cũng ổn. Giá hơi cao nhưng đáng tiền.'),
(1, 3, '2024-02-01 09:45:00', 5, 'Đã sử dụng được 2 tháng, mùi hương vẫn rất thơm. Sẽ mua lại lần nữa.');
