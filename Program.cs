using System;
using System.Data;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AzureSNOMEDCodeUsageDataLoader
{
    class Program
    {
        private static string? _connectionString;
        private static string? _csvFilePath;
        private static string? _tableName;
        private static IConfiguration? _configuration;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure SNOMED Code Usage Data Loader");
            Console.WriteLine("===================================");

            try
            {
                // Load configuration
                LoadConfiguration();

                // Get configuration from user input or command line arguments
                await GetConfiguration(args);

                // Validate inputs
                if (!ValidateInputs())
                {
                    Console.WriteLine("Invalid inputs. Exiting...");
                    return;
                }

                // Load CSV data to Azure database
                await LoadCsvToDatabase();

                Console.WriteLine("Data loading completed successfully!");
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
        }

        private static async Task GetConfiguration(string[] args)
        {
            if (args.Length >= 3)
            {
                // Get from command line arguments
                _connectionString = args[0];
                _csvFilePath = args[1];
                _tableName = args[2];
            }
            else
            {
                // Use defaults from configuration file
                _connectionString = _configuration?.GetConnectionString("AzureDatabase");
                _csvFilePath = _configuration?["Settings:DefaultCsvPath"];
                _tableName = _configuration?["Settings:DefaultTableName"];

                // If any configuration is missing, prompt user
                if (string.IsNullOrWhiteSpace(_connectionString) || 
                    _connectionString.Contains("your-database") || 
                    _connectionString.Contains("your-username") || 
                    _connectionString.Contains("your-password"))
                {
                    Console.WriteLine("Please provide the following information:");
                    Console.Write("Azure SQL Database Connection String: ");
                    _connectionString = Console.ReadLine();
                }
                else
                {
                    Console.WriteLine($"Using connection string from configuration file.");
                }

                if (string.IsNullOrWhiteSpace(_csvFilePath))
                {
                    Console.Write("CSV File Path: ");
                    _csvFilePath = Console.ReadLine();
                }
                else
                {
                    Console.WriteLine($"Using CSV file: {_csvFilePath}");
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

        private static bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                Console.WriteLine("Connection string cannot be empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_csvFilePath) || !File.Exists(_csvFilePath))
            {
                Console.WriteLine("CSV file path is invalid or file does not exist.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_tableName))
            {
                Console.WriteLine("Table name cannot be empty.");
                return false;
            }

            return true;
        }

        private static async Task LoadCsvToDatabase()
        {
            Console.WriteLine($"Loading CSV file: {_csvFilePath}");
            Console.WriteLine($"Target table: {_tableName}");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            Console.WriteLine("Connected to Azure SQL Database successfully.");

            // Read CSV file and get column headers
            var csvData = ReadCsvFile(_csvFilePath);
            var columns = csvData.FirstOrDefault()?.Keys.ToList();

            if (columns == null || columns.Count == 0)
            {
                throw new InvalidOperationException("No columns found in CSV file.");
            }

            Console.WriteLine($"Found {columns.Count} columns: {string.Join(", ", columns)}");

            // Check if table exists, and if so, clear it first
            await ClearTableIfExists(connection, _tableName!);

            // Create table if it doesn't exist
            await CreateTableIfNotExists(connection, _tableName!, columns);

            // Insert data
            await InsertData(connection, _tableName!, csvData);
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

            var tableExists = (int)await checkCommand.ExecuteScalarAsync() > 0;

            if (!tableExists)
            {
                Console.WriteLine($"Table '{tableName}' does not exist. Creating...");

                // Create table with all columns as NVARCHAR(MAX) for simplicity
                var columnDefinitions = columns.Select(col => $"[{col}] NVARCHAR(MAX)").ToList();
                var createTableQuery = $@"
                    CREATE TABLE [{tableName}] (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        {string.Join(",\n                        ", columnDefinitions)}
                    )";

                using var createCommand = new SqlCommand(createTableQuery, connection);
                await createCommand.ExecuteNonQueryAsync();

                Console.WriteLine($"Table '{tableName}' created successfully.");
            }
            else
            {
                Console.WriteLine($"Table '{tableName}' already exists.");
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

            var tableExists = (int)await checkCommand.ExecuteScalarAsync() > 0;

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

        private static async Task InsertData(SqlConnection connection, string tableName, List<Dictionary<string, object>> data)
        {
            if (data.Count == 0)
            {
                Console.WriteLine("No data to insert.");
                return;
            }

            Console.WriteLine($"Inserting {data.Count} records...");

            var columns = data.First().Keys.ToList();
            var columnNames = string.Join(", ", columns.Select(c => $"[{c}]"));
            var parameterNames = string.Join(", ", columns.Select(c => $"@{c}"));

            var insertQuery = $@"
                INSERT INTO [{tableName}] ({columnNames})
                VALUES ({parameterNames})";

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
                        
                        foreach (var column in columns)
                        {
                            var value = record[column];
                            
                            // Handle data type conversions based on column names
                            if (column == "Usage" && value != DBNull.Value)
                            {
                                // Convert Usage to BIGINT, default to 0 if invalid
                                if (long.TryParse(value.ToString(), out long usageValue))
                                {
                                    command.Parameters.AddWithValue($"@{column}", usageValue);
                                }
                                else
                                {
                                    // Use 0 instead of NULL for invalid/empty Usage values
                                    command.Parameters.AddWithValue($"@{column}", 0L);
                                }
                            }
                            else if (column == "Usage" && value == DBNull.Value)
                            {
                                // Use 0 instead of NULL for empty Usage values
                                command.Parameters.AddWithValue($"@{column}", 0L);
                            }
                            else if ((column == "Active_at_Start" || column == "Active_at_End") && value != DBNull.Value)
                            {
                                // Convert Active flags to BIT (0/1)
                                var stringValue = value.ToString()?.ToLower();
                                if (stringValue == "true" || stringValue == "1" || stringValue == "yes")
                                {
                                    command.Parameters.AddWithValue($"@{column}", true);
                                }
                                else if (stringValue == "false" || stringValue == "0" || stringValue == "no")
                                {
                                    command.Parameters.AddWithValue($"@{column}", false);
                                }
                                else
                                {
                                    command.Parameters.AddWithValue($"@{column}", DBNull.Value);
                                }
                            }
                            else
                            {
                                // Keep as string for SNOMED_Concept_ID and Description
                                command.Parameters.AddWithValue($"@{column}", value);
                            }
                        }

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

            Console.WriteLine($"Successfully inserted {insertedCount} records into table '{tableName}'.");
        }
    }
}
