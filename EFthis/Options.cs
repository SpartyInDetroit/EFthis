using CommandLine;

namespace EFthis
{
    public class Options
    {
        [Option('c', "connectionstring", Required = false, HelpText = "SQL Server connection string")]
        public string ConnectionString { get; set; }

        [Option('t', "table", Required = false, HelpText = "table to generate")]
        public string Table { get; set; }

        [Option('s', "schema", Required = false, Default = "dbo", HelpText = "Schema, default is 'dbo'")]
        public string Schema { get; set; }

        [Option('o', "output", Required = false, Default = false, HelpText = "Output directory, required if using YAML input")]
        public bool Output { get; set; }

        [Option(Default = false, HelpText = "Save connection string and schema to local config")]
        public bool Save { get; set; }
    }
}
