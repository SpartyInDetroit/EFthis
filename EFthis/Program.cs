using System;
using System.Data;
using System.Data.SqlClient;
using EFthis.CodeGeneration;

namespace EFthis
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Begin generating entities");

            var connectionString = ""; // INSERT CONNECTION STRING HERE

            IDbConnection connection = new SqlConnection(connectionString);

            var table = "AppUser"; // Fill in table name
            var schema = "dbo";
            var tableMetaData = new TableMetaDataBuilder(connection);
            var tableProperties = tableMetaData.GetProperties(table, schema);

            // Build Entity
            var result = EntityBuilder.BuildEntity(table, schema, tableProperties);

            Console.Write(result);
        }
    }
}
