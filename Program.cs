using System;
using System.Data;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;

namespace AzureSNOMEDCodeUsageDataLoader
{
    class Program
    {
        private static string? _connectionString;
        private static string? _csvFilePath;
        private static string? _tableName;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure SNOMED Code Usage Data Loader");
            Console.WriteLine("===================================");

            try
            {
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
                // Get from user input
                Console.WriteLine("Please provide the following information:");
                
                Console.Write("Azure SQL Database Connection String: ");
                _connectionString = Console.ReadLine();
                
                Console.Write("CSV File Path: ");
                _csvFilePath = Console.ReadLine();
                
                Console.Write("Target Table Name: ");
                _tableName = Console.ReadLine();
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

            // Create table if it doesn't exist
            await CreateTableIfNotExists(connection, _tableName!, columns);

            // Insert data
            await InsertData(connection, _tableName!, csvData);
        }

        private static List<Dictionary<string, object>> ReadCsvFile(string filePath)
        {
            var records = new List<Dictionary<string, object>>();

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null
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
                            command.Parameters.AddWithValue($"@{column}", record[column]);
                        }

                        await command.ExecuteNonQueryAsync();
                        insertedCount++;
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine($"Inserted {insertedCount} of {data.Count} records...");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            Console.WriteLine($"Successfully inserted {insertedCount} records into table '{tableName}'.");
        }
    }
}
