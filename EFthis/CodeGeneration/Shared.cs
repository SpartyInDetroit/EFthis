using System;
using System.Collections.Generic;
using System.Linq;

namespace EFthis.CodeGeneration
{
    public static class Shared
    {
        public static readonly Dictionary<string, string> DataMap = new Dictionary<string, string>
    {
        { "bigint", "long" }
        ,{ "bit", "bool" }
        ,{ "char", "string" }
        ,{ "nchar", "string" }
        ,{ "date", "DateTime" }
        ,{ "datetime", "DateTime" }
        ,{ "datetime2", "DateTime" }
        ,{ "decimal", "decimal" }
        ,{ "float", "decimal" }
        ,{ "hierarchyid", "NotSupported" }
        ,{ "int", "int" }
        ,{ "money", "decimal" }
        ,{ "numeric", "decimal" }
        ,{ "nvarchar", "string" }
        ,{ "smallint", "short" }
        ,{ "time", "TimeSpan" }
        ,{ "uniqueidentifier", "Guid" }
        ,{ "varbinary", "byte[]" }
        ,{ "varchar", "string" }
        ,{ "xml", "string" }
    };

        public static readonly HashSet<string> NonNullableMap = new HashSet<string>
    {
        "bigint"
        , "bit"
        , "date"
        , "datetime"
        , "datetime2"
        , "decimal"
        , "float"
        , "int"
        , "money"
        , "numeric"
        , "smallint"
        , "time"
        , "uniqueidentifier"
    };

        public static readonly HashSet<string> NullableMap = new HashSet<string>
    {
        "char"
        , "nchar"
        , "nvarchar"
        , "varbinary"
        , "varchar"
        , "xml"
    };


        public static string NameSanitize(string name)
        {
            name = name.Replace("_", " ");
            if (name.Contains(" "))
            {
                name = name.Split(new[] { ' ' })
                .Aggregate(string.Empty, (acc, curr) =>
                {
                    if (IsAllUpper(curr))
                    {
                        curr = curr.ToLower();
                    }

                    if (char.IsLower(curr[0]))
                    {
                        curr = char.ToUpper(curr[0]) + curr.Remove(0, 1);
                    }

                    return $"{acc}{curr}";
                });
            }

            if (IsAllUpper(name))
            {
                name = name.ToLower();
                name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
            }

            var upperRun = new List<char>();
            for (int i = 0; i < name.Length; i++)
            {
                var character = name[i];
                if (char.IsUpper(character))
                {
                    upperRun.Add(character);
                    continue;
                }

                if (upperRun.Count < 3)
                {
                    upperRun.Clear();
                    continue;
                }

                var newString = new string(upperRun.ToArray()).ToLower();
                newString = char.ToUpper(newString[0]) + newString.Remove(0, 1);
                newString = newString.Remove(newString.Length - 1, 1) + char.ToUpper(newString[newString.Length - 1]);
                name = name.Replace(new string(upperRun.ToArray()), newString);
                upperRun.Clear();
            }

            if (upperRun.Count > 1)
            {
                var newString = new string(upperRun.ToArray()).ToLower();
                newString = char.ToUpper(newString[0]) + newString.Remove(0, 1);
                name = name.Replace(new string(upperRun.ToArray()), newString);
            }

            return name;
        }

        private static bool IsAllUpper(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (Char.IsLetter(input[i]) && !Char.IsUpper(input[i]))
                    return false;
            }
            return true;
        }

    }
}
