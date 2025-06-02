# Azure SNOMED Code Usage Data Loader

A C# console application for loading CSV files into an Azure SQL Database. This tool is specifically designed for SNOMED code usage data but can be used with any CSV file.

## Features

- **CSV File Processing**: Reads any CSV file with headers
- **Automatic Table Creation**: Creates database tables automatically based on CSV structure
- **Batch Processing**: Efficiently processes large files in batches (default 1000 records)
- **Transaction Safety**: Uses database transactions to ensure data integrity
- **Flexible Input**: Accepts configuration via command line arguments or interactive prompts
- **Error Handling**: Comprehensive error handling and logging

## Prerequisites

- .NET 9.0 or later
- Azure SQL Database or SQL Server instance
- Valid CSV file with headers

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
2. CSV File Path
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
- CSV File Path
- Target Table Name

### Method 3: Publish and Run Executable
```bash
dotnet publish -c Release
cd bin/Release/net9.0/publish
./AzureSNOMEDCodeUsageDataLoader.exe
```

## How It Works

1. **Connection**: Establishes connection to Azure SQL Database
2. **CSV Reading**: Reads the CSV file and extracts column headers
3. **Table Creation**: Creates a table with the specified name if it doesn't exist
   - Adds an auto-incrementing `Id` column as primary key
   - Creates columns based on CSV headers as `NVARCHAR(MAX)`
4. **Data Insertion**: Inserts data in batches with transaction safety
5. **Progress Reporting**: Shows progress throughout the process

## Example CSV Structure

Your CSV file should have headers in the first row:

```csv
ConceptId,Term,Usage_Count,Last_Used_Date
123456789,Hypertension,450,2025-01-15
987654321,Diabetes mellitus,320,2025-01-14
```

This will create a table with columns:
- `Id` (auto-increment primary key)
- `ConceptId`
- `Term`
- `Usage_Count`
- `Last_Used_Date`

## Error Handling

The application includes comprehensive error handling for:
- Invalid connection strings
- Missing or inaccessible CSV files
- Database connection issues
- SQL execution errors
- CSV parsing errors

## Performance Considerations

- **Batch Size**: Default batch size is 1000 records. Adjust in `appsettings.json` if needed
- **Memory Usage**: The application loads the entire CSV into memory for processing
- **Large Files**: For very large files (>100MB), consider splitting them into smaller chunks

## Dependencies

- **Microsoft.Data.SqlClient** (6.0.2): Azure SQL Database connectivity
- **CsvHelper** (33.1.0): CSV file processing

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License.

## Support

For issues or questions, please create an issue on the GitHub repository.