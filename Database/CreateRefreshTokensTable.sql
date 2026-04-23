-- =============================================
-- RefreshTokens Table Creation Script
-- =============================================
-- Database: LearningDB
-- Description: Creates RefreshTokens table for JWT Authentication API
-- =============================================

USE [LearningDB]
GO

-- =============================================
-- Drop RefreshTokens table if it exists
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RefreshTokens]') AND type in (N'U'))
BEGIN
    PRINT 'Dropping existing RefreshTokens table...'
    DROP TABLE [dbo].[RefreshTokens]
    PRINT 'RefreshTokens table dropped.'
END
GO

-- =============================================
-- Create RefreshTokens Table
-- =============================================
PRINT 'Creating RefreshTokens table...'
GO

CREATE TABLE [dbo].[RefreshTokens] (
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Token] NVARCHAR(500) NOT NULL,
    [ExpiresAt] DATETIME2 NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETUTCDATE()),
    [IsRevoked] BIT NOT NULL DEFAULT 0,
    [RevokedBy] NVARCHAR(256) NULL,
    [RevokedAt] DATETIME2 NULL,
    [ReplacedByToken] NVARCHAR(500) NULL,
    
    -- Primary Key
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([Id] ASC),
    
    -- Foreign Key to Users table
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[Users] ([Id]) 
        ON DELETE CASCADE
)
GO

-- =============================================
-- Create Indexes for Performance
-- =============================================
PRINT 'Creating indexes...'
GO

-- Index on Token for fast lookups during refresh
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_Token] 
ON [dbo].[RefreshTokens] ([Token] ASC)
GO

-- Index on UserId for user-specific queries
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] 
ON [dbo].[RefreshTokens] ([UserId] ASC)
GO

-- Index on ExpiresAt for cleanup operations
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_ExpiresAt] 
ON [dbo].[RefreshTokens] ([ExpiresAt] ASC)
GO

-- Composite index for active token queries
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId_IsRevoked_ExpiresAt] 
ON [dbo].[RefreshTokens] ([UserId] ASC, [IsRevoked] ASC, [ExpiresAt] ASC)
GO

-- =============================================
-- Table Information
-- =============================================
PRINT ''
PRINT 'RefreshTokens table created successfully!'
PRINT ''
PRINT 'Table Structure:'
PRINT '  - Id: Unique identifier for each refresh token'
PRINT '  - UserId: Foreign key to Users table'
PRINT '  - Token: The actual refresh token value (indexed)'
PRINT '  - ExpiresAt: Token expiration date/time'
PRINT '  - CreatedAt: Token creation timestamp (default: UTC now)'
PRINT '  - IsRevoked: Token revocation status (default: false)'
PRINT '  - RevokedBy: User/system that revoked the token'
PRINT '  - RevokedAt: Revocation timestamp'
PRINT '  - ReplacedByToken: New token if this was rotated'
PRINT ''
PRINT 'Indexes Created:'
PRINT '  - IX_RefreshTokens_Token (for fast token lookups)'
PRINT '  - IX_RefreshTokens_UserId (for user queries)'
PRINT '  - IX_RefreshTokens_ExpiresAt (for cleanup operations)'
PRINT '  - IX_RefreshTokens_UserId_IsRevoked_ExpiresAt (composite index)'
PRINT ''
GO

-- =============================================
-- Verification Query
-- =============================================
SELECT 
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    dc.definition AS DefaultValue
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
WHERE t.name = 'RefreshTokens'
ORDER BY c.column_id
GO

PRINT ''
PRINT 'Setup complete!'
GO
