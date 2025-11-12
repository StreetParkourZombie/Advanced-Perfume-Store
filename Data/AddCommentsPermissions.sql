-- Add Permissions for Comments Management
-- Run this script to add permissions for the Comments module

USE PerfumeStore;
GO

-- Insert permissions if they don't exist
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'View Comments')
BEGIN
    INSERT INTO Permissions (Name, Action, Area, Description)
    VALUES ('View Comments', 'Index', 'Admin', 'Xem danh sách bình luận');
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Create Comment')
BEGIN
    INSERT INTO Permissions (Name, Action, Area, Description)
    VALUES ('Create Comment', 'Create', 'Admin', 'Tạo bình luận mới');
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Edit Comment')
BEGIN
    INSERT INTO Permissions (Name, Action, Area, Description)
    VALUES ('Edit Comment', 'Edit', 'Admin', 'Chỉnh sửa bình luận');
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Approve Comment')
BEGIN
    INSERT INTO Permissions (Name, Action, Area, Description)
    VALUES ('Approve Comment', 'Approve', 'Admin', 'Phê duyệt bình luận');
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Hide Comment')
BEGIN
    INSERT INTO Permissions (Name, Action, Area, Description)
    VALUES ('Hide Comment', 'Hide', 'Admin', 'Ẩn bình luận');
END

-- Grant all Comments permissions to Admin role (RoleID = 1)
DECLARE @AdminRoleId INT = 1;

-- Get permission IDs
DECLARE @ViewCommentsId INT, @CreateCommentId INT, @EditCommentId INT, @ApproveCommentId INT, @HideCommentId INT;

SELECT @ViewCommentsId = PermissionId FROM Permissions WHERE Name = 'View Comments';
SELECT @CreateCommentId = PermissionId FROM Permissions WHERE Name = 'Create Comment';
SELECT @EditCommentId = PermissionId FROM Permissions WHERE Name = 'Edit Comment';
SELECT @ApproveCommentId = PermissionId FROM Permissions WHERE Name = 'Approve Comment';
SELECT @HideCommentId = PermissionId FROM Permissions WHERE Name = 'Hide Comment';

-- Insert into RolePermissions if not exists
IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleID = @AdminRoleId AND PermissionId = @ViewCommentsId)
BEGIN
    INSERT INTO RolePermissions (RoleID, PermissionId) VALUES (@AdminRoleId, @ViewCommentsId);
END

IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleID = @AdminRoleId AND PermissionId = @CreateCommentId)
BEGIN
    INSERT INTO RolePermissions (RoleID, PermissionId) VALUES (@AdminRoleId, @CreateCommentId);
END

IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleID = @AdminRoleId AND PermissionId = @EditCommentId)
BEGIN
    INSERT INTO RolePermissions (RoleID, PermissionId) VALUES (@AdminRoleId, @EditCommentId);
END

IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleID = @AdminRoleId AND PermissionId = @ApproveCommentId)
BEGIN
    INSERT INTO RolePermissions (RoleID, PermissionId) VALUES (@AdminRoleId, @ApproveCommentId);
END

IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleID = @AdminRoleId AND PermissionId = @HideCommentId)
BEGIN
    INSERT INTO RolePermissions (RoleID, PermissionId) VALUES (@AdminRoleId, @HideCommentId);
END

-- Verify
SELECT 
    p.Name,
    p.Action,
    p.Area,
    p.Description,
    CASE WHEN rp.PermissionId IS NOT NULL THEN 'Yes' ELSE 'No' END AS 'Assigned to Admin'
FROM Permissions p
LEFT JOIN RolePermissions rp ON p.PermissionId = rp.PermissionId AND rp.RoleID = @AdminRoleId
WHERE p.Name LIKE '%Comment%'
ORDER BY p.Name;

PRINT 'Comments permissions added successfully!';
GO



