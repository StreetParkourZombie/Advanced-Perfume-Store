-- Add Permissions for Brand Management
-- Run this script to add permissions for the Brand module

USE PerfumeStore;
GO

-- Insert permissions if they don't exist
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'View Brands')
BEGIN
    INSERT INTO Permissions (Name, Action, Area, Description)
    VALUES ('View Brands', 'Index', 'Admin', 'Xem danh sách thương hiệu');
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Create Brand')
BEGIN
    INSERT INTO Permissions (Name, Action, Area, Description)
    VALUES ('Create Brand', 'Create', 'Admin', 'Tạo thương hiệu mới');
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Edit Brand')
BEGIN
    INSERT INTO Permissions (Name, Action, Area, Description)
    VALUES ('Edit Brand', 'Edit', 'Admin', 'Chỉnh sửa thương hiệu');
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Delete Brand')
BEGIN
    INSERT INTO Permissions (Name, Action, Area, Description)
    VALUES ('Delete Brand', 'Delete', 'Admin', 'Xóa thương hiệu');
END

-- Grant all Brand permissions to Admin role (RoleID = 1)
DECLARE @AdminRoleId INT = 1;

-- Get permission IDs
DECLARE @ViewBrandsId INT, @CreateBrandId INT, @EditBrandId INT, @DeleteBrandId INT;

SELECT @ViewBrandsId = PermissionId FROM Permissions WHERE Name = 'View Brands';
SELECT @CreateBrandId = PermissionId FROM Permissions WHERE Name = 'Create Brand';
SELECT @EditBrandId = PermissionId FROM Permissions WHERE Name = 'Edit Brand';
SELECT @DeleteBrandId = PermissionId FROM Permissions WHERE Name = 'Delete Brand';

-- Insert into RolePermissions if not exists
IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleID = @AdminRoleId AND PermissionId = @ViewBrandsId)
BEGIN
    INSERT INTO RolePermissions (RoleID, PermissionId) VALUES (@AdminRoleId, @ViewBrandsId);
END

IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleID = @AdminRoleId AND PermissionId = @CreateBrandId)
BEGIN
    INSERT INTO RolePermissions (RoleID, PermissionId) VALUES (@AdminRoleId, @CreateBrandId);
END

IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleID = @AdminRoleId AND PermissionId = @EditBrandId)
BEGIN
    INSERT INTO RolePermissions (RoleID, PermissionId) VALUES (@AdminRoleId, @EditBrandId);
END

IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleID = @AdminRoleId AND PermissionId = @DeleteBrandId)
BEGIN
    INSERT INTO RolePermissions (RoleID, PermissionId) VALUES (@AdminRoleId, @DeleteBrandId);
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
WHERE p.Name LIKE '%Brand%'
ORDER BY p.Name;

PRINT 'Brand permissions added successfully!';
GO

