using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace EFthis.CodeGeneration
{
    public class TableMetaDataBuilder
    {
        private readonly IDbConnection _connection;

        public TableMetaDataBuilder(IDbConnection connection)
        {
            _connection = connection;

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        public List<TableProperty> GetProperties(string table, string schema)
        {
            const string sql = @" -- get columns query
				SELECT
					c.COLUMN_NAME AS ColumnName
					, c.ORDINAL_POSITION AS Position
					, c.IS_NULLABLE AS IsNullable
					, c.DATA_TYPE AS DataType
					, c.CHARACTER_MAXIMUM_LENGTH AS Size
					, c.NUMERIC_PRECISION AS Precision
					, c.NUMERIC_SCALE AS Scale
				FROM    INFORMATION_SCHEMA.COLUMNS c
				WHERE   c.TABLE_NAME = @TableName and ISNULL(@TableSchema, c.TABLE_SCHEMA) = c.TABLE_SCHEMA  
				ORDER BY c.ORDINAL_POSITION";

            var pks = GetPk(table, schema);
            var computeds = GetComputed(table, schema);
            var identities = GetIdentity(table, schema);
            var fks = GetFks(table, schema);
            var dependentTables = GetDependentTables(table, schema);

            var properties = new List<TableProperty>();
            using (var cmd = new SqlCommand(sql, (SqlConnection)_connection))
            {
                cmd.Parameters.Add(new SqlParameter("@TableName", table));
                cmd.Parameters.Add(new SqlParameter("@TableSchema", schema));

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var size = default(int?);
                        if (int.TryParse(reader["Size"].ToString(), out var tmpSize))
                        {
                            size = tmpSize;
                        }

                        var precision = default(int?);
                        if (int.TryParse(reader["Precision"].ToString(), out var tmpPrecision))
                        {
                            precision = tmpPrecision;
                        }

                        var scale = default(int?);
                        if (int.TryParse(reader["Scale"].ToString(), out var tmpScale))
                        {
                            scale = tmpScale;
                        }

                        var colName = reader["ColumnName"].ToString();

                        var prop = new TableProperty
                        {
                            ColumnName = colName,
                            Position = int.Parse(reader["Position"].ToString()),
                            DataType = reader["DataType"].ToString(),
                            PrimaryTable = fks
                                .Where(x => x.column == colName)
                                .Select(x => x.table)
                                .FirstOrDefault(),
                            DependentTables = dependentTables
                                .Where(x => x.refColumn == colName)
                                .Select(x => x.dependentTable)
                                .ToList(),
                            IsNullable = reader["IsNullable"].ToString() == "YES",
                            Size = size,
                            Precision = precision,
                            Scale = scale,
                            IsComputed = computeds.Contains(colName),
                            IsIdentity = identities.Contains(colName),
                            IsKey = pks.Contains(colName),
                        };

                        properties.Add(prop);
                    }
                }
            }

            return properties;
        }

        private List<string> GetPk(string table, string schema)
        {
            const string sql = @" -- PK columns
				SELECT ku.COLUMN_NAME AS Name
				FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
				INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS ku
				    ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY' 
				    AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
					AND tc.CONSTRAINT_SCHEMA = ku.CONSTRAINT_SCHEMA
					AND tc.TABLE_CATALOG = ku.TABLE_CATALOG
					AND tc.TABLE_NAME = ku.TABLE_NAME
				WHERE ku.TABLE_NAME = @TableName and ISNULL(@TableSchema, ku.TABLE_SCHEMA) = ku.TABLE_SCHEMA";

            var properties = new List<string>();
            using (var cmd = new SqlCommand(sql, (SqlConnection)_connection))
            {
                cmd.Parameters.Add(new SqlParameter("@TableName", table));
                cmd.Parameters.Add(new SqlParameter("@TableSchema", schema));

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        properties.Add(reader["Name"].ToString());
                    }
                }
            }

            return properties;
        }

        private List<string> GetIdentity(string table, string schema)
        {
            const string sql = @" -- identity columns
				select COLUMN_NAME AS Name
				from INFORMATION_SCHEMA.COLUMNS
				where COLUMNPROPERTY(object_id('AspNetUsers'), COLUMN_NAME, 'IsIdentity') = 1
				AND TABLE_NAME = @TableName and ISNULL(@TableSchema, TABLE_SCHEMA) = TABLE_SCHEMA";

            var properties = new List<string>();
            using (var cmd = new SqlCommand(sql, (SqlConnection)_connection))
            {
                cmd.Parameters.Add(new SqlParameter("@TableName", table));
                cmd.Parameters.Add(new SqlParameter("@TableSchema", schema));

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        properties.Add(reader["Name"].ToString());
                    }
                }
            }

            return properties;
        }

        private List<string> GetComputed(string table, string schema)
        {
            const string sql = @" -- computed columns
				SELECT Name
				FROM sys.computed_columns
				WHERE object_id = OBJECT_ID(@TableSchema + '.' + @TableName)";

            var properties = new List<string>();
            using (var cmd = new SqlCommand(sql, (SqlConnection)_connection))
            {
                cmd.Parameters.Add(new SqlParameter("@TableName", table));
                cmd.Parameters.Add(new SqlParameter("@TableSchema", schema));

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        properties.Add(reader["Name"].ToString());
                    }
                }
            }

            return properties;
        }

        private List<(string dependentTable, string refColumn)> GetDependentTables(string table, string schema)
        {
            const string sql = @" -- Dependent tables
				SELECT
				   OBJECT_NAME(f.parent_object_id) AS DependentTable,
				   col.name AS ReferenceColumn
				FROM 
				   sys.foreign_keys AS f
				INNER JOIN 
				   sys.foreign_key_columns AS fc 
				      ON f.OBJECT_ID = fc.constraint_object_id
				INNER JOIN 
				   sys.tables t 
				      ON t.OBJECT_ID = f.referenced_object_id
				INNER JOIN
					sys.columns col
					  ON col.object_id = t.object_id AND column_id = fc.referenced_column_id
				WHERE 
				   f.referenced_object_id = OBJECT_ID(@TableSchema + '.' + @TableName)";

            var properties = new List<(string, string)>();
            using (var cmd = new SqlCommand(sql, (SqlConnection)_connection))
            {
                cmd.Parameters.Add(new SqlParameter("@TableName", table));
                cmd.Parameters.Add(new SqlParameter("@TableSchema", schema));

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        properties.Add((reader["DependentTable"].ToString(), reader["ReferenceColumn"].ToString()));
                    }
                }
            }

            return properties;
        }

        private List<(string column, string table)> GetFks(string table, string schema)
        {
            const string sql = @" -- fk columns
				select 
				    col.name as ColumnName,
				    pk_tab.name as PrimaryTable
				from sys.tables tab
				    inner join sys.columns col 
				        on col.object_id = tab.object_id
				    left outer join sys.foreign_key_columns fk_cols
				        on fk_cols.parent_object_id = tab.object_id
				        and fk_cols.parent_column_id = col.column_id
				    left outer join sys.foreign_keys fk
				        on fk.object_id = fk_cols.constraint_object_id
				    left outer join sys.tables pk_tab
				        on pk_tab.object_id = fk_cols.referenced_object_id
				    left outer join sys.columns pk_col
				        on pk_col.column_id = fk_cols.referenced_column_id
				        and pk_col.object_id = fk_cols.referenced_object_id
				where fk.object_id is not null AND tab.name = @TableName
					AND ISNULL(@TableSchema, schema_name(tab.schema_id)) = schema_name(tab.schema_id)
				order by schema_name(tab.schema_id) + '.' + tab.name,
				    col.column_id";

            var properties = new List<(string, string)>();
            using (var cmd = new SqlCommand(sql, (SqlConnection)_connection))
            {
                cmd.Parameters.Add(new SqlParameter("@TableName", table));
                cmd.Parameters.Add(new SqlParameter("@TableSchema", schema));

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        properties.Add((reader["ColumnName"].ToString(), reader["PrimaryTable"].ToString()));
                    }
                }
            }

            return properties;
        }
    }
}
