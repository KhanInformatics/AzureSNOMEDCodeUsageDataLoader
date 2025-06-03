-- SQL Script to create SNOMED Code Usage table in Azure SQL Database
-- Database: SNOMEDCodeUsage
-- Table for NHS Digital SNOMED Code Usage in Primary Care data

-- IMPORTANT: You MUST connect directly to the 'SNOMEDCodeUsage' database before running this script
-- In Azure SQL Database, you cannot use USE statements to switch databases
-- Make sure your connection string points to: Server=your-server.database.windows.net;Database=SNOMEDCodeUsage;

-- Verify you're connected to the correct database
SELECT DB_NAME() AS CurrentDatabase;
GO

-- Only proceed if you're in the SNOMEDCodeUsage database
IF DB_NAME() != 'SNOMEDCodeUsage'
BEGIN
    PRINT 'ERROR: You are connected to the wrong database!'
    PRINT 'Current database: ' + DB_NAME()
    PRINT 'Please connect to the SNOMEDCodeUsage database before running this script.'
    RETURN
END
GO

-- Drop table if it exists
IF OBJECT_ID('dbo.SNOMED_Usage_Data', 'U') IS NOT NULL
    DROP TABLE dbo.SNOMED_Usage_Data;
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SNOMED_Usage_Data](
	[SNOMED_Concept_ID] [nvarchar](18) NOT NULL,
	[Description] [nvarchar](500) NOT NULL,
	[Usage] [bigint] NOT NULL,
	[Active_at_Start] [bit] NOT NULL,
	[Active_at_End] [bit] NOT NULL,
	[Created_Date] [datetime2](7) NULL,
	[Data_Period] [nvarchar](10) NULL,
	[Geographic_Coverage] [nvarchar](20) NULL
) ON [PRIMARY]
GO

SET ANSI_PADDING ON
GO
ALTER TABLE [dbo].[SNOMED_Usage_Data] ADD  CONSTRAINT [PK_SNOMED_Usage_Data] PRIMARY KEY CLUSTERED 
(
	[SNOMED_Concept_ID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_SNOMED_Usage_Data_Active] ON [dbo].[SNOMED_Usage_Data]
(
	[Active_at_Start] ASC,
	[Active_at_End] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_SNOMED_Usage_Data_Usage] ON [dbo].[SNOMED_Usage_Data]
(
	[Usage] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

ALTER TABLE [dbo].[SNOMED_Usage_Data] ADD  DEFAULT (getdate()) FOR [Created_Date]
GO

ALTER TABLE [dbo].[SNOMED_Usage_Data] ADD  DEFAULT ('2023-24') FOR [Data_Period]
GO

ALTER TABLE [dbo].[SNOMED_Usage_Data] ADD  DEFAULT ('England') FOR [Geographic_Coverage]
GO

-- Add comments/extended properties for documentation
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SNOMED CT concept identifier' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SNOMED_Usage_Data', @level2type=N'COLUMN',@level2name=N'SNOMED_Concept_ID'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Human readable description of the SNOMED code' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SNOMED_Usage_Data', @level2type=N'COLUMN',@level2name=N'Description'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Number of times code was added to GP patient records' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SNOMED_Usage_Data', @level2type=N'COLUMN',@level2name=N'Usage'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether code was active at start of period (1=Yes, 0=No)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SNOMED_Usage_Data', @level2type=N'COLUMN',@level2name=N'Active_at_Start'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether code was active at end of period (1=Yes, 0=No)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SNOMED_Usage_Data', @level2type=N'COLUMN',@level2name=N'Active_at_End'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'NHS Digital SNOMED Code Usage in Primary Care data for England, period 2023-24' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SNOMED_Usage_Data'
GO

PRINT 'SNOMED_Usage_Data table created successfully';
PRINT 'Table includes:'
PRINT '- Primary key on SNOMED_Concept_ID'
PRINT '- Index on Usage (descending) for performance'
PRINT '- Index on Active_at_Start and Active_at_End'
PRINT '- Default values for metadata columns'
PRINT '- Extended properties for documentation'
GO
