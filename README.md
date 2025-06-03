# Azure SNOMED Code Usage Data Loader

A comprehensive solution for loading NHS Digital SNOMED CT Code Usage data into Azure SQL Database. This project includes both database table creation scripts and a C# data loading application specifically designed for SNOMED code usage statistics from primary care in England.

## Project Structure

```
AzureSNOMEDCodeUsageDataLoader/
├── Program.cs                     # Main C# application
├── AzureSNOMEDCodeUsageDataLoader.csproj  # Project file
├── appsettings.json              # Configuration file
├── CreateSNOMEDUsageTable.sql    # Database table creation script
├── README.md                     # This documentation
├── SNOMED_code_usage_2023-24.txt # Sample SNOMED data file
└── sample-data.csv               # Sample CSV data for testing
```

## Quick Start

### Step 1: Create the Database Table
Before loading data, create the SNOMED usage table in your Azure SQL Database:

1. Connect to your Azure SQL Database using SQL Server Management Studio, Azure Data Studio, or similar tool
2. Run the SQL script: `CreateSNOMEDUsageTable.sql`

This creates a properly structured table with:
- Primary key on SNOMED_Concept_ID
- Optimized indexes for performance
- Proper data types (BIGINT for usage counts, BIT for flags)
- Metadata columns with default values
- Extended properties for documentation

### Step 2: Configure and Run the Data Loader
1. Update `appsettings.json` with your database connection details
2. Run the application: `dotnet run`
3. The application will load all SNOMED code usage data into the database

## Features

- **SNOMED CT Optimized**: Specifically designed for NHS Digital SNOMED code usage data with proper data type handling
- **Database Table Creation**: Includes SQL script to create properly structured database table
- **Automatic Data Type Conversion**: Converts Usage values to BIGINT and Active flags to BIT
- **Table Clearing**: Automatically clears existing data before loading new data to prevent duplicates
- **Flexible File Processing**: Automatically detects and handles both comma-separated (CSV) and tab-separated (TSV) files
- **Batch Processing**: Efficiently processes large files in batches (default 1000 records)
- **Transaction Safety**: Uses database transactions to ensure data integrity
- **Flexible Input**: Accepts configuration via command line arguments or interactive prompts
- **Error Handling**: Comprehensive error handling and progress reporting

## Database Schema

The `CreateSNOMEDUsageTable.sql` script creates a table with the following structure:

| Column | Data Type | Description |
|--------|-----------|-------------|
| SNOMED_Concept_ID | NVARCHAR(18) | SNOMED CT concept identifier (Primary Key) |
| Description | NVARCHAR(500) | Human readable description of the SNOMED code |
| Usage | BIGINT | Number of times code was added to GP patient records |
| Active_at_Start | BIT | Whether code was active at start of period (1=Yes, 0=No) |
| Active_at_End | BIT | Whether code was active at end of period (1=Yes, 0=No) |
| Created_Date | DATETIME2(7) | Timestamp when record was created (default: current date) |
| Data_Period | NVARCHAR(10) | Data period identifier (default: '2023-24') |
| Geographic_Coverage | NVARCHAR(20) | Geographic coverage (default: 'England') |

### Performance Indexes
- **Primary Key**: Clustered index on SNOMED_Concept_ID
- **Usage Index**: Non-clustered index on Usage (descending) for performance queries
- **Active Status Index**: Non-clustered index on Active_at_Start and Active_at_End

## Prerequisites

- .NET 9.0 or later
- Azure SQL Database or SQL Server instance
- Valid CSV/TSV file with headers

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/KhanInformatics/AzureSNOMEDCodeUsageDataLoader.git
   ```

2. Navigate to the project directory:
   ```bash
   cd AzureSNOMEDCodeUsageDataLoader
   ```

3. Restore dependencies:
   ```bash
   dotnet restore
   ```

4. Build the project:
   ```bash
   dotnet build
   ```

## SNOMED Data Format

The application is specifically designed for NHS Digital SNOMED CT Code Usage data files, which are tab-separated (TSV) with the following structure:

### Expected TSV Format
```
SNOMED_Concept_ID	Description	Usage	Active_at_Start	Active_at_End
279991000000102	Short message service text message sent to patient (procedure)	440821890	1	1
184103008	Patient telephone number (observable entity)	191021270	1	1
428481002	Patient mobile telephone number (observable entity)	115964490	1	1
```

**Column Descriptions:**
- **SNOMED_Concept_ID**: Unique SNOMED CT concept identifier
- **Description**: Human-readable clinical term description
- **Usage**: Number of times the code was used in GP patient records during the period
- **Active_at_Start**: Whether the code was active at the start of the data period (1=Yes, 0=No)
- **Active_at_End**: Whether the code was active at the end of the data period (1=Yes, 0=No)

### Data Source
This format matches the NHS Digital SNOMED CT Code Usage in Primary Care dataset, which provides annual statistics on SNOMED CT concept usage across GP practices in England.

## Configuration

### Option 1: Update appsettings.json
Edit the `appsettings.json` file with your Azure SQL Database connection string:

```json
{
  "ConnectionStrings": {
    "AzureDatabase": "Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=yourdatabase;Persist Security Info=False;User ID=yourusername;Password=yourpassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

### Option 2: Command Line Arguments
You can provide configuration via command line arguments in this order:
1. Connection String
2. CSV/TSV File Path
3. Table Name

### Option 3: Interactive Mode
Run the application without arguments and it will prompt for the required information.

## Usage

### Method 1: Command Line Arguments
```bash
dotnet run "Server=tcp:yourserver.database.windows.net,1433;..." "path/to/your/file.csv" "TableName"
```

### Method 2: Interactive Mode
```bash
dotnet run
```
Then follow the prompts to enter:
- Azure SQL Database Connection String
- CSV/TSV File Path
- Target Table Name

### Method 3: Publish and Run Executable
```bash
dotnet publish -c Release
cd bin/Release/net9.0/publish
./AzureSNOMEDCodeUsageDataLoader.exe
```

## Complete Workflow

### 1. Database Setup
```sql
-- Connect to your Azure SQL Database and run:
sqlcmd -S your-server.database.windows.net -d SNOMEDCodeUsage -U your-username -P your-password -i CreateSNOMEDUsageTable.sql
```

### 2. Application Configuration
Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "AzureDatabase": "Server=tcp:snomedctsql.database.windows.net,1433;Initial Catalog=SNOMEDCodeUsage;..."
  },
  "Settings": {
    "DefaultTableName": "SNOMED_Usage_Data",
    "BatchSize": 1000,
    "DefaultCsvPath": "SNOMED_code_usage_2023-24.txt"
  }
}
```

### 3. Data Loading
```bash
dotnet run
```

The application will:
1. Connect to your Azure SQL Database
2. Clear existing data from the SNOMED_Usage_Data table
3. Load all 157,000+ SNOMED code usage records
4. Show progress as it processes data in batches

## Example Output

```
Azure SNOMED Code Usage Data Loader
===================================
Using connection string from configuration file.
Using CSV file: SNOMED_code_usage_2023-24.txt
Using table name: SNOMED_Usage_Data
Loading CSV file: SNOMED_code_usage_2023-24.txt
Target table: SNOMED_Usage_Data
Connected to Azure SQL Database successfully.
Detected delimiter: Tab
Read 157454 records from CSV file.
Found 5 columns: SNOMED_Concept_ID, Description, Usage, Active_at_Start, Active_at_End
Checking if table 'SNOMED_Usage_Data' exists for clearing...
Table 'SNOMED_Usage_Data' exists. Clearing data...
Table 'SNOMED_Usage_Data' cleared successfully.
Inserting 157454 records...
Inserted 1000 of 157454 records...
Inserted 2000 of 157454 records...
...
Successfully inserted 157454 records into table 'SNOMED_Usage_Data'.
Data loading completed successfully!
```

## Error Handling

The application includes comprehensive error handling for:
- Invalid connection strings
- Missing or inaccessible CSV/TSV files
- Database connection issues
- SQL execution errors
- CSV/TSV parsing errors

## Performance Considerations

- **Batch Size**: Default batch size is 1000 records. Adjust in `appsettings.json` if needed
- **Memory Usage**: The application loads the entire CSV/TSV into memory for processing
- **Large Files**: For very large files (>100MB), consider splitting them into smaller chunks

## Dependencies

- **.NET 9.0**: Modern .NET runtime for high performance
- **Microsoft.Data.SqlClient** (6.0.2): Azure SQL Database connectivity with advanced features
- **CsvHelper** (33.1.0): Robust CSV/TSV file processing library
- **Microsoft.Extensions.Configuration** (8.0.0): Configuration management
- **Microsoft.Extensions.Configuration.Json** (8.0.0): JSON configuration file support

## Files Included

- **Program.cs**: Main application logic with data type conversion and batch processing
- **CreateSNOMEDUsageTable.sql**: Complete database table creation script
- **appsettings.json**: Configuration file with connection strings and settings
- **SNOMED_code_usage_2023-24.txt**: Sample NHS Digital SNOMED usage data file
- **sample-data.csv**: Sample CSV data for testing

## Performance Notes

- Successfully tested with 157,454 SNOMED code records
- Processes approximately 1,000 records per second
- Uses transaction batching for optimal performance and data integrity
- Includes proper data type conversion for large integers and boolean flags
- Automatically handles missing or invalid data values

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License.

## Support

For issues or questions, please create an issue on the GitHub repository.