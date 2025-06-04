using System;
using System.Data;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AzureSNOMEDCodeUsageDataLoader
{    class Program
    {
        private static string? _connectionString;
        private static string? _sourceDataFolder;
        private static string? _tableName;
        private static IConfiguration? _configuration;static async Task Main(string[] args)
        {
            Console.WriteLine("Azure SNOMED Code Usage Data Loader - Multi-File Processor");
            Console.WriteLine("==========================================================");

            try
            {
                // Load configuration
                LoadConfiguration();

                // Get configuration from user input or command line arguments
                GetConfiguration(args);

                // Validate inputs
                if (!ValidateInputs())
                {
                    Console.WriteLine("Invalid inputs. Exiting...");
                    return;
                }

                // Process all files in the source data folder
                await ProcessAllFiles();

                Console.WriteLine("\nAll data loading completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }        private static void GetConfiguration(string[] args)
        {
            if (args.Length >= 3)
            {
                // Get from command line arguments
                _connectionString = args[0];
                _sourceDataFolder = args[1];
                _tableName = args[2];
            }
            else
            {
                // Use defaults from configuration file
                _connectionString = _configuration?.GetConnectionString("LocalDatabase");
                _sourceDataFolder = _configuration?["Settings:SourceDataFolder"];
                _tableName = _configuration?["Settings:DefaultTableName"];

                // If any configuration is missing, prompt user
                if (string.IsNullOrWhiteSpace(_connectionString) || 
                    _connectionString.Contains("your-database") || 
                    _connectionString.Contains("your-username") || 
                    _connectionString.Contains("your-password"))
                {
                    Console.WriteLine("Please provide the following information:");
                    Console.Write("SQL Server Connection String: ");
                    _connectionString = Console.ReadLine();
                }
                else
                {
                    Console.WriteLine($"Using connection string from configuration file.");
                }

                if (string.IsNullOrWhiteSpace(_sourceDataFolder))
                {
                    Console.Write("Source Data Folder Path: ");
                    _sourceDataFolder = Console.ReadLine();
                }
                else
                {
                    Console.WriteLine($"Using source data folder: {_sourceDataFolder}");
                }

                if (string.IsNullOrWhiteSpace(_tableName))
                {
                    Console.Write("Target Table Name: ");
                    _tableName = Console.ReadLine();
                }
                else
                {
                    Console.WriteLine($"Using table name: {_tableName}");
                }
            }
        }

        private static string DetectDataPeriodFromFilename(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Look for pattern like "2023-24" or "2022-23" in filename
            var periodMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d{4}-\d{2})");
            
            if (periodMatch.Success)
            {
                return periodMatch.Groups[1].Value;
            }
            
            // Default fallback
            return "2023-24";
        }

        private static string GetDataHandlingChoice(string dataPeriod)
        {
            Console.WriteLine("\nData Handling Options:");
            Console.WriteLine("1. Clear all existing data and load new data (TRUNCATE)");
            Console.WriteLine("2. Delete data for this period only and load new data");
            Console.WriteLine("3. Add data alongside existing data (may cause primary key conflicts)");
            Console.WriteLine("4. Cancel loading");
            
            while (true)
            {
                Console.Write($"\nHow would you like to handle existing data for period {dataPeriod}? (1-4): ");
                var choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        return "truncate";
                    case "2":
                        return "delete_period";
                    case "3":
                        return "append";
                    case "4":
                        Environment.Exit(0);
                        return "";
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1, 2, 3, or 4.");
                        continue;
                }
            }
        }        private static bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                Console.WriteLine("Connection string cannot be empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_sourceDataFolder) || !Directory.Exists(_sourceDataFolder))
            {
                Console.WriteLine("Source data folder path is invalid or directory does not exist.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_tableName))
            {
                Console.WriteLine("Table name cannot be empty.");
                return false;
            }

            return true;
        }

        private static async Task ProcessAllFiles()
        {
            Console.WriteLine($"\nScanning source data folder: {_sourceDataFolder}");
            
            // Get all SNOMED files from the source folder
            var snomedFiles = Directory.GetFiles(_sourceDataFolder!, "SNOMED_code_usage_*.txt")
                .OrderBy(f => f)
                .ToList();

            if (snomedFiles.Count == 0)
            {
                Console.WriteLine("No SNOMED usage files found in the source folder.");
                Console.WriteLine("Expected files with pattern: SNOMED_code_usage_*.txt");
                return;
            }

            Console.WriteLine($"Found {snomedFiles.Count} SNOMED usage files:");
            foreach (var file in snomedFiles)
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }

            // Ask user for data handling choice once for all files
            Console.WriteLine("\nData Handling Options for ALL files:");
            Console.WriteLine("1. Clear all existing data and load all files (TRUNCATE)");
            Console.WriteLine("2. Delete data by period for each file and load (SMART REPLACE)");
            Console.WriteLine("3. Add data alongside existing data (may cause primary key conflicts)");
            Console.WriteLine("4. Cancel loading");
            
            string globalChoice;
            while (true)
            {
                Console.Write("\nHow would you like to handle existing data? (1-4): ");
                var choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        globalChoice = "truncate";
                        break;
                    case "2":
                        globalChoice = "delete_period";
                        break;
                    case "3":
                        globalChoice = "append";
                        break;
                    case "4":
                        Environment.Exit(0);
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1, 2, 3, or 4.");
                        continue;
                }
                break;
            }

            // If truncate is chosen, do it once at the beginning
            bool tableCleared = false;

            // Process each file
            for (int i = 0; i < snomedFiles.Count; i++)
            {
                var filePath = snomedFiles[i];
                var fileName = Path.GetFileName(filePath);
                var dataPeriod = DetectDataPeriodFromFilename(filePath);

                Console.WriteLine($"\n=== Processing file {i + 1} of {snomedFiles.Count}: {fileName} ===");
                Console.WriteLine($"Detected data period: {dataPeriod}");

                var effectiveChoice = globalChoice;
                
                // If truncate was chosen, only do it for the first file
                if (globalChoice == "truncate")
                {
                    if (!tableCleared)
                    {
                        effectiveChoice = "truncate";
                        tableCleared = true;
                    }
                    else
                    {
                        effectiveChoice = "append";
                    }
                }

                try
                {
                    await LoadCsvToDatabase(filePath, dataPeriod, effectiveChoice);
                    Console.WriteLine($"✓ Successfully processed {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error processing {fileName}: {ex.Message}");
                    
                    Console.Write("Continue with remaining files? (y/n): ");
                    var continueChoice = Console.ReadLine()?.ToLower();
                    if (continueChoice != "y" && continueChoice != "yes")
                    {
                        Console.WriteLine("Processing stopped by user.");
                        return;
                    }
                }
            }
        }

        private static async Task LoadCsvToDatabase(string csvFilePath, string dataPeriod, string clearChoice)        {
            Console.WriteLine($"Loading CSV file: {csvFilePath}");
            Console.WriteLine($"Target table: {_tableName}");
            Console.WriteLine($"Data period: {dataPeriod}");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            Console.WriteLine("Connected to SQL Server successfully.");

            // Read CSV file and get column headers
            var csvData = ReadCsvFile(csvFilePath);
            var columns = csvData.FirstOrDefault()?.Keys.ToList();

            if (columns == null || columns.Count == 0)
            {
                throw new InvalidOperationException("No columns found in CSV file.");
            }

            Console.WriteLine($"Found {columns.Count} columns: {string.Join(", ", columns)}");

            // Handle existing data based on user choice
            await HandleExistingData(connection, _tableName!, dataPeriod, clearChoice);

            // Create table if it doesn't exist
            await CreateTableIfNotExists(connection, _tableName!, columns);

            // Insert data with the detected data period
            await InsertData(connection, _tableName!, csvData, dataPeriod);
        }

        private static async Task HandleExistingData(SqlConnection connection, string tableName, string dataPeriod, string clearChoice)
        {
            Console.WriteLine($"Handling existing data with choice: {clearChoice}");

            switch (clearChoice)
            {
                case "truncate":
                    await ClearTableIfExists(connection, tableName);
                    break;
                    
                case "delete_period":
                    await DeleteDataForPeriod(connection, tableName, dataPeriod);
                    break;
                    
                case "append":
                    Console.WriteLine("Appending data - no existing data will be removed.");
                    Console.WriteLine("WARNING: This may cause primary key conflicts if the same SNOMED IDs exist for this period.");
                    break;
                    
                default:
                    throw new ArgumentException($"Invalid clear choice: {clearChoice}");
            }
        }

        private static async Task DeleteDataForPeriod(SqlConnection connection, string tableName, string dataPeriod)
        {
            Console.WriteLine($"Checking if table '{tableName}' exists for period-specific deletion...");

            // Check if table exists
            var checkTableQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @TableName";

            using var checkCommand = new SqlCommand(checkTableQuery, connection);
            checkCommand.Parameters.AddWithValue("@TableName", tableName);

            var result = await checkCommand.ExecuteScalarAsync();
            var tableExists = result != null && (int)result > 0;

            if (tableExists)
            {
                // Check if Data_Period column exists
                var checkColumnQuery = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'Data_Period'";

                using var checkColumnCommand = new SqlCommand(checkColumnQuery, connection);
                checkColumnCommand.Parameters.AddWithValue("@TableName", tableName);

                var columnResult = await checkColumnCommand.ExecuteScalarAsync();
                var columnExists = columnResult != null && (int)columnResult > 0;

                if (columnExists)
                {
                    var deleteQuery = $@"DELETE FROM [{tableName}] WHERE [Data_Period] = @DataPeriod";
                    using var deleteCommand = new SqlCommand(deleteQuery, connection);
                    deleteCommand.Parameters.AddWithValue("@DataPeriod", dataPeriod);

                    var deletedRows = await deleteCommand.ExecuteNonQueryAsync();
                    Console.WriteLine($"Deleted {deletedRows} existing records for period '{dataPeriod}' from table '{tableName}'.");
                }
                else
                {
                    Console.WriteLine($"Data_Period column does not exist in table '{tableName}'. Cannot delete by period.");
                    Console.WriteLine("Consider using option 1 (truncate) or 3 (append) instead.");
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine($"Table '{tableName}' does not exist, no need to delete data.");
            }
        }

        private static List<Dictionary<string, object>> ReadCsvFile(string filePath)
        {
            var records = new List<Dictionary<string, object>>();

            using var reader = new StreamReader(filePath);
            
            // Detect delimiter by reading the first line
            var firstLine = reader.ReadLine();
            reader.BaseStream.Position = 0;
            reader.DiscardBufferedData();
            
            var delimiter = firstLine?.Contains('\t') == true ? "\t" : ",";
            Console.WriteLine($"Detected delimiter: {(delimiter == "\t" ? "Tab" : "Comma")}");

            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                Delimiter = delimiter
            });

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;

            if (headers == null)
            {
                throw new InvalidOperationException("No headers found in CSV file.");
            }

            while (csv.Read())
            {
                var record = new Dictionary<string, object>();
                foreach (var header in headers)
                {
                    var value = csv.GetField(header);
                    record[header] = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
                }
                records.Add(record);
            }

            Console.WriteLine($"Read {records.Count} records from CSV file.");
            return records;
        }

        private static async Task CreateTableIfNotExists(SqlConnection connection, string tableName, List<string> columns)
        {
            Console.WriteLine($"Checking if table '{tableName}' exists...");

            // Check if table exists
            var checkTableQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @TableName";

            using var checkCommand = new SqlCommand(checkTableQuery, connection);
            checkCommand.Parameters.AddWithValue("@TableName", tableName);

            var result = await checkCommand.ExecuteScalarAsync();
            var tableExists = result != null && (int)result > 0;            if (!tableExists)
            {
                Console.WriteLine($"Table '{tableName}' does not exist.");
                Console.WriteLine("Please create the table using the provided SQL script first.");
                Console.WriteLine("The table should have the multi-year structure with composite primary key.");
                throw new InvalidOperationException($"Table '{tableName}' does not exist. Please create it first.");
            }
            else
            {
                Console.WriteLine($"Table '{tableName}' already exists with proper multi-year structure.");
                
                // Verify the table has the required columns
                var checkColumnsQuery = @"
                    SELECT COLUMN_NAME 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName 
                    ORDER BY ORDINAL_POSITION";

                using var checkColumnsCommand = new SqlCommand(checkColumnsQuery, connection);
                checkColumnsCommand.Parameters.AddWithValue("@TableName", tableName);

                var existingColumns = new List<string>();
                using var reader = await checkColumnsCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    existingColumns.Add(reader.GetString(0));
                }

                Console.WriteLine($"Table columns: {string.Join(", ", existingColumns)}");
                
                // Check for required columns
                var requiredColumns = new[] { "SNOMED_Concept_ID", "Description", "Usage", "Active_at_Start", "Active_at_End", "Data_Period" };
                var missingColumns = requiredColumns.Where(col => !existingColumns.Contains(col, StringComparer.OrdinalIgnoreCase)).ToList();
                
                if (missingColumns.Any())
                {
                    throw new InvalidOperationException($"Table is missing required columns: {string.Join(", ", missingColumns)}");
                }
            }
        }

        private static async Task ClearTableIfExists(SqlConnection connection, string tableName)
        {
            Console.WriteLine($"Checking if table '{tableName}' exists for clearing...");

            // Check if table exists
            var checkTableQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @TableName";

            using var checkCommand = new SqlCommand(checkTableQuery, connection);
            checkCommand.Parameters.AddWithValue("@TableName", tableName);

            var result = await checkCommand.ExecuteScalarAsync();
            var tableExists = result != null && (int)result > 0;

            if (tableExists)
            {
                Console.WriteLine($"Table '{tableName}' exists. Clearing data...");

                var truncateTableQuery = $@"TRUNCATE TABLE [{tableName}]";

                using var truncateCommand = new SqlCommand(truncateTableQuery, connection);
                await truncateCommand.ExecuteNonQueryAsync();

                Console.WriteLine($"Table '{tableName}' cleared successfully.");
            }
            else
            {
                Console.WriteLine($"Table '{tableName}' does not exist, no need to clear.");
            }
        }

        private static async Task InsertData(SqlConnection connection, string tableName, List<Dictionary<string, object>> data, string dataPeriod)
        {
            if (data.Count == 0)
            {
                Console.WriteLine("No data to insert.");
                return;
            }            Console.WriteLine($"Inserting {data.Count} records for period {dataPeriod}...");

            // Map CSV columns to table structure
            // CSV has: SNOMED_Concept_ID, Description, Usage, Active_at_Start, Active_at_End
            // Table has: SNOMED_Concept_ID, Description, Usage, Active_at_Start, Active_at_End, Created_Date, Data_Period, Geographic_Coverage
            
            var insertQuery = $@"
                INSERT INTO [{tableName}] 
                ([SNOMED_Concept_ID], [Description], [Usage], [Active_at_Start], [Active_at_End], [Data_Period], [Created_Date], [Geographic_Coverage])
                VALUES 
                (@SNOMED_Concept_ID, @Description, @Usage, @Active_at_Start, @Active_at_End, @Data_Period, @Created_Date, @Geographic_Coverage)";

            var insertedCount = 0;
            var batchSize = 1000;

            for (int i = 0; i < data.Count; i += batchSize)
            {
                var batch = data.Skip(i).Take(batchSize);
                
                using var transaction = connection.BeginTransaction();
                try
                {
                    foreach (var record in batch)
                    {
                        using var command = new SqlCommand(insertQuery, connection, transaction);
                        
                        // Map CSV columns to table columns
                        command.Parameters.AddWithValue("@SNOMED_Concept_ID", record["SNOMED_Concept_ID"] ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Description", record["Description"] ?? DBNull.Value);
                        
                        // Handle Usage conversion to BIGINT
                        if (record["Usage"] != DBNull.Value && long.TryParse(record["Usage"].ToString(), out long usageValue))
                        {
                            command.Parameters.AddWithValue("@Usage", usageValue);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@Usage", 0L);
                        }
                        
                        // Handle Active flags conversion to BIT
                        command.Parameters.AddWithValue("@Active_at_Start", ConvertToBit(record["Active_at_Start"]));
                        command.Parameters.AddWithValue("@Active_at_End", ConvertToBit(record["Active_at_End"]));
                        
                        // Add computed/default columns
                        command.Parameters.AddWithValue("@Data_Period", dataPeriod);
                        command.Parameters.AddWithValue("@Created_Date", DateTime.Now);
                        command.Parameters.AddWithValue("@Geographic_Coverage", "England");

                        await command.ExecuteNonQueryAsync();
                        insertedCount++;
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine($"Inserted {insertedCount} of {data.Count} records...");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error in batch starting at record {i + 1}: {ex.Message}");
                    throw;
                }
            }

            Console.WriteLine($"Successfully inserted {insertedCount} records into table '{tableName}' for period '{dataPeriod}'.");
        }

        private static bool ConvertToBit(object value)
        {
            if (value == DBNull.Value || value == null)
                return false;
                  var stringValue = value.ToString()?.ToLower();
            return stringValue == "true" || stringValue == "1" || stringValue == "yes";
        }
    }
}
