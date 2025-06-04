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
	[Data_Period] [nvarchar](10) NOT NULL,
	[Geographic_Coverage] [nvarchar](20) NULL
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [dbo].[SNOMED_Usage_Data] ADD  CONSTRAINT [PK_SNOMED_Usage_Data] PRIMARY KEY CLUSTERED 
(
	[SNOMED_Concept_ID] ASC,
	[Data_Period] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_SNOMED_Usage_Data_Active] ON [dbo].[SNOMED_Usage_Data]
(
	[Data_Period] ASC,
	[Active_at_Start] ASC,
	[Active_at_End] ASC
)
INCLUDE([SNOMED_Concept_ID],[Description],[Usage]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_SNOMED_Usage_Data_Usage] ON [dbo].[SNOMED_Usage_Data]
(
	[Data_Period] ASC,
	[Usage] DESC
)
INCLUDE([SNOMED_Concept_ID],[Description]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[SNOMED_Usage_Data] ADD  DEFAULT (getdate()) FOR [Created_Date]
GO
ALTER TABLE [dbo].[SNOMED_Usage_Data] ADD  DEFAULT ('2023-24') FOR [Data_Period]
GO
ALTER TABLE [dbo].[SNOMED_Usage_Data] ADD  DEFAULT ('England') FOR [Geographic_Coverage]
GO
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
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Data period identifier - part of composite primary key to support multiple years' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SNOMED_Usage_Data', @level2type=N'COLUMN',@level2name=N'Data_Period'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'NHS Digital SNOMED Code Usage in Primary Care data for England, period 2023-24' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SNOMED_Usage_Data'
GO
