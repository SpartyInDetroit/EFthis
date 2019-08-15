using System.Collections.Generic;

namespace EFthis.CodeGeneration
{
    public class TableProperty
    {
        public string ColumnName { get; set; }
        public int Position { get; set; }
        public bool IsNullable { get; set; }
        public string DataType { get; set; }
        public string PrimaryTable { get; set; }
        public List<string> DependentTables { get; set; }
        public int? Size { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public bool IsKey { get; set; }
        public bool IsComputed { get; set; }
        public bool IsIdentity { get; set; }
    }
}
