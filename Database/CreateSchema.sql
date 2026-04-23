-- =============================================
-- JWT Authentication API Database Schema
-- =============================================
-- Database: LearningDB
-- Description: Complete schema for JWT Authentication with Refresh Tokens
-- =============================================

USE [LearningDB]
GO

-- =============================================
-- Drop existing tables if they exist (for clean setup)
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RefreshTokens]') AND type in (N'U'))
DROP TABLE [dbo].[RefreshTokens]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
DROP TABLE [dbo].[Users]
GO

-- =============================================
-- Create Users Table
-- =============================================
CREATE TABLE [dbo].[Users] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Email] NVARCHAR(256) NOT NULL,
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [Role] NVARCHAR(50) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETUTCDATE()),
    [IsActive] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK__Users__3214EC0780F98538] PRIMARY KEY ([Id])
)
GO

-- =============================================
-- Create Indexes for Users Table
-- =============================================
CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users] ([Email])
GO

-- =============================================
-- Create RefreshTokens Table
-- =============================================
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
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
)
GO

-- =============================================
-- Create Indexes for RefreshTokens Table
-- =============================================
CREATE INDEX [IX_RefreshTokens_Token] ON [dbo].[RefreshTokens] ([Token])
GO

CREATE INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens] ([UserId])
GO

CREATE INDEX [IX_RefreshTokens_ExpiresAt] ON [dbo].[RefreshTokens] ([ExpiresAt])
GO

-- =============================================
-- Create Migration History Table (for EF Core)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    )
END
GO

-- =============================================
-- Insert Migration History Record
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260415022331_AddRefreshTokenSupport')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260415022331_AddRefreshTokenSupport', N'10.0.5')
END
GO

-- =============================================
-- Verification Queries
-- =============================================
PRINT 'Database Schema Created Successfully!'
PRINT ''
PRINT 'Verifying Tables...'

SELECT 'Users' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Users]
UNION ALL
SELECT 'RefreshTokens' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[RefreshTokens]
GO

PRINT ''
PRINT 'Schema creation complete!'
GO
