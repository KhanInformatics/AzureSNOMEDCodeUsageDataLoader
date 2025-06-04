# Multi-File Processing Implementation Summary

## Overview
Successfully implemented multi-file processing functionality for the SNOMED Code Usage Data Loader. The application now automatically processes all SNOMED usage files from a designated source folder.

## Key Features Implemented

### 1. Automatic File Discovery
- Scans the `SNOMEDUsageSourceData` folder for files matching pattern `SNOMED_code_usage_*.txt`
- Processes files in alphabetical order
- Displays list of discovered files before processing

### 2. Intelligent Data Period Detection
- Automatically extracts data period from filename (e.g., "2022-23", "2023-24")
- Uses regex pattern `(\d{4}-\d{2})` to detect periods
- Falls back to "2023-24" if no pattern found

### 3. Flexible Data Handling Options
- **Option 1: TRUNCATE** - Clears all existing data, then loads all files
- **Option 2: SMART REPLACE** - Deletes existing data by period, then loads new data
- **Option 3: APPEND** - Adds data alongside existing records (may cause conflicts)
- **Option 4: CANCEL** - Exits without making changes

### 4. Batch Processing with Error Handling
- Processes files one by one with individual error handling
- Option to continue or stop processing if errors occur
- Comprehensive progress reporting
- Transaction-based batch inserts (1000 records per batch)

## Files Modified

### Program.cs
- Replaced single file processing with multi-file batch processing
- Added `ProcessAllFiles()` method for automated processing
- Updated `GetConfiguration()` to use source folder instead of single file path
- Modified `LoadCsvToDatabase()` to accept file path parameter
- Updated `ValidateInputs()` for folder validation instead of file validation

### appsettings.json
- Changed `DefaultCsvPath` to `SourceDataFolder` setting
- Set default folder to `SNOMEDUsageSourceData`

## Successfully Loaded Data

### Data Summary
- **Total Records**: 306,888
- **2022-23 Period**: 149,434 records
- **2023-24 Period**: 157,454 records
- **Load Time**: ~2 minutes total
- **Database**: Local SQL Server Express (`silentpriory\SQLEXPRESS`)
- **Table**: `SNOMED_Usage_Data` with composite primary key

### Table Structure
```sql
SNOMED_Concept_ID (BIGINT)
Description (NVARCHAR(500))
Usage (BIGINT)
Active_at_Start (BIT)
Active_at_End (BIT)
Created_Date (DATETIME2)
Data_Period (NVARCHAR(20))
Geographic_Coverage (NVARCHAR(50))

PRIMARY KEY: (SNOMED_Concept_ID, Data_Period)
```

## Usage Instructions

### Command Line
```powershell
cd "o:\GitHub\AzureLoader\AzureSNOMEDCodeUsageDataLoader"
dotnet run
```

### Interactive Process
1. Application loads configuration from `appsettings.json`
2. Scans `SNOMEDUsageSourceData` folder for SNOMED files
3. Lists discovered files and asks for data handling preference
4. Processes each file with period-specific handling
5. Reports success/failure for each file
6. Displays final completion summary

### Adding New Files
1. Place new SNOMED usage files in `SNOMEDUsageSourceData` folder
2. Ensure filename follows pattern: `SNOMED_code_usage_YYYY-YY.txt`
3. Run the application and choose appropriate data handling option

## Error Handling
- Connection validation before processing
- File format validation (tab vs comma delimiter detection)
- Data type conversion for Usage (BIGINT) and Active flags (BIT)
- Transaction rollback on batch failures
- Continue/stop options on individual file failures

## Performance
- Batch processing: 1000 records per transaction
- Progress reporting every 1000 records
- Efficient memory usage with streaming CSV reader
- Transaction-based error recovery

## Future Enhancements
- Configuration for custom file patterns
- Parallel file processing option
- Data validation and reporting
- Export functionality for processed data
- Automated scheduling capabilities
