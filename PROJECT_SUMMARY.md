# Project Summary

## Azure SNOMED Code Usage Data Loader

This repository contains a complete C# console application for loading CSV files into Azure SQL Database with multi-year data support.

### What's Been Created:

1. **Core Application** (`Program.cs`)
   - Full-featured CSV to Azure SQL Database loader
   - Interactive and command-line modes
   - Automatic table creation based on CSV structure
   - Batch processing with transaction safety
   - Comprehensive error handling
   - **Multi-year data support with automatic period detection**
   - **Flexible data handling options (truncate, period-specific deletion, append)**

2. **Dependencies Added**
   - Microsoft.Data.SqlClient (6.0.2) - Azure SQL connectivity
   - CsvHelper (33.1.0) - CSV file processing

3. **Configuration Files**
   - `appsettings.json` - Configuration template
   - `.gitignore` - Proper exclusions for .NET projects

4. **SQL Scripts**
   - `CreateSNOMEDUsageTable.sql` - Original table creation script
   - `ModifyTableForMultiYear.sql` - Table modification for multi-year support

5. **Data Files**
   - `SNOMED_code_usage_2023-24.txt` - Current period data (149,436 records)
   - `SNOMED_code_usage_2022-23.txt` - Previous period data (149,436 records)

6. **Git Repository**
   - Initialized with proper .gitignore
   - Initial commit completed
   - Remote origin set to: https://github.com/KhanInformatics/AzureSNOMEDCodeUsageDataLoader.git

### Next Steps:

1. **For First-Time Setup:**
   - Update `appsettings.json` with your Azure SQL Database connection string
   - Run `ModifyTableForMultiYear.sql` if you have existing data to add multi-year support

2. **For Loading New Data:**
   - The application automatically detects data period from filename (e.g., "2022-23")
   - Choose data handling option when prompted:
     - Option 1: Clear all data and load new (TRUNCATE)
     - Option 2: Delete specific period data and load new
     - Option 3: Append data (may cause conflicts)
     - Option 4: Cancel loading

3. **Test the Multi-Year Functionality:**
   - Run with `SNOMED_code_usage_2022-23.txt` to test period detection
   - Verify data loading with different handling options

### Usage Examples:

```bash
# Interactive mode (recommended for multi-year data)
dotnet run

# Command line mode
dotnet run "Server=tcp:yourserver.database.windows.net,1433;..." "SNOMED_code_usage_2022-23.txt" "SNOMED_Code_Usage"

# Build for distribution
dotnet publish -c Release
```

### Multi-Year Features:

- **Automatic Period Detection**: Extracts data period from filename (e.g., "2022-23", "2023-24")
- **Composite Primary Key**: SNOMED_Concept_ID + Data_Period allows same concepts across years
- **Flexible Data Management**: Choose how to handle existing data when loading new periods
- **Period-Specific Operations**: Delete only specific period data without affecting others

### Project Status: ✅ Complete with Multi-Year Support Ready for Use