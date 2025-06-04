-- Script to modify SNOMED_Usage_Data table to support multiple years
-- This changes the primary key to include Data_Period

USE [SNOMEDCodeUsage]
GO

-- Step 1: Drop existing primary key constraint
ALTER TABLE [dbo].[SNOMED_Usage_Data] DROP CONSTRAINT [PK_SNOMED_Usage_Data]
GO

-- Step 2: Make Data_Period NOT NULL (required for primary key)
-- First update any existing NULL values
UPDATE [dbo].[SNOMED_Usage_Data] 
SET [Data_Period] = '2023-24' 
WHERE [Data_Period] IS NULL
GO

-- Alter column to NOT NULL
ALTER TABLE [dbo].[SNOMED_Usage_Data] 
ALTER COLUMN [Data_Period] [nvarchar](10) NOT NULL
GO

-- Step 3: Create new composite primary key
ALTER TABLE [dbo].[SNOMED_Usage_Data] 
ADD CONSTRAINT [PK_SNOMED_Usage_Data] PRIMARY KEY CLUSTERED 
(
    [SNOMED_Concept_ID] ASC,
    [Data_Period] ASC
)
GO

-- Step 4: Update indexes to include Data_Period for better performance
DROP INDEX [IX_SNOMED_Usage_Data_Active] ON [dbo].[SNOMED_Usage_Data]
GO

CREATE NONCLUSTERED INDEX [IX_SNOMED_Usage_Data_Active] ON [dbo].[SNOMED_Usage_Data]
(
    [Data_Period] ASC,
    [Active_at_Start] ASC,
    [Active_at_End] ASC
)
INCLUDE ([SNOMED_Concept_ID], [Description], [Usage])
GO

DROP INDEX [IX_SNOMED_Usage_Data_Usage] ON [dbo].[SNOMED_Usage_Data]
GO

CREATE NONCLUSTERED INDEX [IX_SNOMED_Usage_Data_Usage] ON [dbo].[SNOMED_Usage_Data]
(
    [Data_Period] ASC,
    [Usage] DESC
)
INCLUDE ([SNOMED_Concept_ID], [Description])
GO

-- Add extended property to document the change
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Data period identifier - part of composite primary key to support multiple years' , 
    @level0type=N'SCHEMA',@level0name=N'dbo', 
    @level1type=N'TABLE',@level1name=N'SNOMED_Usage_Data', 
    @level2type=N'COLUMN',@level2name=N'Data_Period'
GO

PRINT 'Table successfully modified to support multiple years of data'
PRINT 'Primary key is now: SNOMED_Concept_ID + Data_Period'
GO
