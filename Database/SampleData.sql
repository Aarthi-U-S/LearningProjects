-- =============================================
-- Sample Data for JWT Authentication API
-- =============================================
-- Database: LearningDB
-- Description: Insert sample users and test data
-- =============================================

USE [LearningDB]
GO

-- =============================================
-- Insert Sample Users
-- =============================================
-- Note: Passwords are hashed using BCrypt
-- Sample passwords (for reference):
--   Admin123! (for admin user)
--   User123! (for regular user)
--   Test123! (for test user)
-- =============================================

PRINT 'Inserting sample users...'
GO

-- Admin User
-- Password: Admin123!
-- BCrypt Hash: $2a$11$5B5B5B5B5B5B5B5B5B5B5OhKQqF8zRqZ8zRqZ8zRqZ8zRqZ8zRqZ (example - generate real hash)
INSERT INTO [dbo].[Users] ([Id], [Email], [PasswordHash], [Role], [CreatedAt], [IsActive])
VALUES 
(
    NEWID(),
    'admin@example.com',
    '$2a$11$XQzVR7K9C0HLqKZ7K9C0HuyMZqF8zRqZ8zRqZ8zRqZ8zRqZ8zRqZ',
    'Admin',
    GETUTCDATE(),
    1
)
GO

-- Regular User
-- Password: User123!
INSERT INTO [dbo].[Users] ([Id], [Email], [PasswordHash], [Role], [CreatedAt], [IsActive])
VALUES 
(
    NEWID(),
    'user@example.com',
    '$2a$11$YRzVR7K9C0HLqKZ7K9C0HuyMZqF8zRqZ8zRqZ8zRqZ8zRqZ8zRqZ',
    'User',
    GETUTCDATE(),
    1
)
GO

-- Test User
-- Password: Test123!
INSERT INTO [dbo].[Users] ([Id], [Email], [PasswordHash], [Role], [CreatedAt], [IsActive])
VALUES 
(
    NEWID(),
    'test@example.com',
    '$2a$11$ZRzVR7K9C0HLqKZ7K9C0HuyMZqF8zRqZ8zRqZ8zRqZ8zRqZ8zRqZ',
    'User',
    GETUTCDATE(),
    1
)
GO

-- Inactive User (for testing account deactivation)
INSERT INTO [dbo].[Users] ([Id], [Email], [PasswordHash], [Role], [CreatedAt], [IsActive])
VALUES 
(
    NEWID(),
    'inactive@example.com',
    '$2a$11$ARzVR7K9C0HLqKZ7K9C0HuyMZqF8zRqZ8zRqZ8zRqZ8zRqZ8zRqZ',
    'User',
    GETUTCDATE(),
    0
)
GO

-- =============================================
-- Verification
-- =============================================
PRINT ''
PRINT 'Sample data inserted successfully!'
PRINT ''
PRINT 'Users in database:'
SELECT 
    [Email],
    [Role],
    [IsActive],
    [CreatedAt]
FROM [dbo].[Users]
ORDER BY [CreatedAt] DESC
GO

-- =============================================
-- IMPORTANT NOTES
-- =============================================
-- The password hashes above are examples only!
-- To create real users, use the API's register endpoint:
--
-- Example:
-- POST /api/auth/register
-- {
--   "email": "admin@example.com",
--   "password": "Admin123!",
--   "role": "Admin"
-- }
--
-- This will generate proper BCrypt hashes.
-- =============================================
