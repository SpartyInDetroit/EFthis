using System;
using System.Data.SqlClient;
using CommandLine;
using EFthis.CodeGeneration;

namespace EFthis
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    ControlFlow(options);
                });
        }

        private static void ControlFlow(Options options)
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                Console.WriteLine("Connection string or YAML input required");
                return;
            }

            BuildSingleEntity(options.ConnectionString, options.Table, options.Schema, options.Output);
        }

        private static void BuildSingleEntity(string connectionString, string table, string schema, bool output)
        {
            if (output)
            {
                Console.WriteLine("Output to directory not implemented yet");
            }

            try
            {
                var connection = new SqlConnection(connectionString);
                var tableMetadata = new TableMetaDataBuilder(connection);
                var tableProperties = tableMetadata.GetProperties(table, schema);
                var entity = EntityBuilder.BuildEntity(table, schema, tableProperties);

                Console.Write(entity);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something went wrong:{Environment.NewLine}{e}");
            }
        }
    }
}