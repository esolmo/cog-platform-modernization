-- ============================================================
-- Seed: Sport Types for COGDB_Betting
-- Target table : [COGDB_Betting].[dbo].[SportTypes]
-- Idempotent   : INSERT only if Code does not already exist
-- Matches      : BettingService/src/Migrations/20260409120000_InitialCreate.cs InsertData
-- ============================================================

SET NOCOUNT ON;
USE [COGDB_Betting];
GO

-- Disable IDENTITY_INSERT so EF Core migration IDs and SQL script IDs stay in sync.
-- If the table was created by EF Core migrations the rows are already present;
-- these guards make this script safe to re-run.

IF NOT EXISTS (SELECT 1 FROM [dbo].[SportTypes] WHERE [Code] = 'NFL')
    INSERT INTO [dbo].[SportTypes] ([Name], [Code], [IsActive], [DisplayOrder])
    VALUES ('Football (NFL)', 'NFL', 1, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SportTypes] WHERE [Code] = 'NCAAF')
    INSERT INTO [dbo].[SportTypes] ([Name], [Code], [IsActive], [DisplayOrder])
    VALUES ('Football (NCAAF)', 'NCAAF', 1, 2);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SportTypes] WHERE [Code] = 'NBA')
    INSERT INTO [dbo].[SportTypes] ([Name], [Code], [IsActive], [DisplayOrder])
    VALUES ('Basketball (NBA)', 'NBA', 1, 3);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SportTypes] WHERE [Code] = 'NCAAB')
    INSERT INTO [dbo].[SportTypes] ([Name], [Code], [IsActive], [DisplayOrder])
    VALUES ('Basketball (NCAAB)', 'NCAAB', 1, 4);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SportTypes] WHERE [Code] = 'MLB')
    INSERT INTO [dbo].[SportTypes] ([Name], [Code], [IsActive], [DisplayOrder])
    VALUES ('Baseball (MLB)', 'MLB', 1, 5);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SportTypes] WHERE [Code] = 'NHL')
    INSERT INTO [dbo].[SportTypes] ([Name], [Code], [IsActive], [DisplayOrder])
    VALUES ('Hockey (NHL)', 'NHL', 1, 6);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SportTypes] WHERE [Code] = 'SOC')
    INSERT INTO [dbo].[SportTypes] ([Name], [Code], [IsActive], [DisplayOrder])
    VALUES ('Soccer', 'SOC', 1, 7);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SportTypes] WHERE [Code] = 'FIGHT')
    INSERT INTO [dbo].[SportTypes] ([Name], [Code], [IsActive], [DisplayOrder])
    VALUES ('Boxing / MMA', 'FIGHT', 1, 8);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SportTypes] WHERE [Code] = 'TEN')
    INSERT INTO [dbo].[SportTypes] ([Name], [Code], [IsActive], [DisplayOrder])
    VALUES ('Tennis', 'TEN', 1, 9);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SportTypes] WHERE [Code] = 'GOLF')
    INSERT INTO [dbo].[SportTypes] ([Name], [Code], [IsActive], [DisplayOrder])
    VALUES ('Golf', 'GOLF', 1, 10);

PRINT 'Seeded SportTypes for COGDB_Betting.';
GO
