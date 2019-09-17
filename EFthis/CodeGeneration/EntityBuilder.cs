using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFthis.CodeGeneration
{
    public static class EntityBuilder
    {
        public static string BuildEntity(string tableName, string schema, ICollection<TableProperty> properties)
        {
            var sb = new StringBuilder($"\t[Table(\"{tableName}\", Schema = \"{schema}\")]\r\n");
            sb.Append($"\tpublic class {Shared.NameSanitize(tableName)}\r\n\t{{\r\n");

            bool firstProp = true;
            foreach (var prop in properties)
            {
                WriteProperty(sb, prop, properties, firstProp);
                firstProp = false;
            }

            var navProperties = properties
                .Where(x => !string.IsNullOrWhiteSpace(x.PrimaryTable))
                .ToList();

            if (navProperties.Any())
            {
                sb.Append("\r\n\t\t/* Start Nav Properties\r\n");

                foreach (var prop in navProperties)
                {
                    sb.Append($"\t\t[ForeignKey(\"{Shared.NameSanitize(prop.ColumnName)}\")]\r\n");
                    sb.Append($"\t\tpublic virtual {Shared.NameSanitize(prop.PrimaryTable)} {Shared.NameSanitize(prop.PrimaryTable)} {{ get; set; }}\r\n");
                }

                sb.Append("\t\tEnd Nav Properties */\r\n");
            }

            var dependentTables = properties
                .SelectMany(x => x.DependentTables)
                .ToList();

            if (dependentTables.Any())
            {
                sb.Append("\r\n\t\t/* Start Collection Nav Properties\r\n");

                foreach (var prop in dependentTables)
                {
                    sb.Append($"\t\tpublic virtual ICollection<{Shared.NameSanitize(prop)}> {Shared.NameSanitize(prop)} {{ get; set; }} = new HashSet<{Shared.NameSanitize(prop)}>();\r\n");
                }

                sb.Append("\t\tEnd Collection Nav Properties */\r\n");
            }

            sb.Append("\t}");

            return sb.ToString();
        }

        private static void WriteProperty(StringBuilder sb, TableProperty prop, ICollection<TableProperty> properties, bool firstProp)
        {
            var attributes = GetPropertyAttributes(prop, properties);
            if (attributes.Any() && !firstProp)
            {
                sb.Append("\r\n");
            }

            sb.Append("\t\t");
            foreach (var attr in attributes)
            {
                sb.Append($"[{attr.Name}");

                var defaultParam = attr.Params.SingleOrDefault(x => string.IsNullOrWhiteSpace(x.Key));
                var attrParams = attr.Params
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                    .Aggregate(defaultParam.Value ?? string.Empty, (acc, cur) => $"{acc}, {cur.Key} = {cur.Value}")
                    .Trim(new[] { ',', ' ' });

                if (attrParams.Length > 0)
                {
                    sb.Append($"({attrParams})");
                }

                sb.Append("]\r\n\t\t");
            }

            sb.Append($"public {Shared.DataMap[prop.DataType]}");

            if (prop.IsNullable && Shared.NonNullableMap.Contains(prop.DataType))
            {
                sb.Append("?");
            }

            sb.Append($" {Shared.NameSanitize(prop.ColumnName)} {{ get; set; }}");
            sb.Append("\r\n");
        }


        private static ICollection<AttrMetaData> GetPropertyAttributes(TableProperty prop, ICollection<TableProperty> properties)
        {
            return AttrMetaDataBuilder.GetPropertyAttributes(prop, properties);
        }

        private class AttrMetaDataBuilder
        {
            private static readonly HashSet<string> UnicodeTypes = new HashSet<string>
            {
                "nvarchar", "nchar"
            };

            private static readonly HashSet<string> MinLengthTypes = new HashSet<string>
            {
                "char", "nchar"
            };

            public static ICollection<AttrMetaData> GetPropertyAttributes(TableProperty prop, ICollection<TableProperty> properties)
            {
                var attributes = new List<AttrMetaData>();

                void MaybeAddAttr(Func<TableProperty, ICollection<TableProperty>, ICollection<AttrMetaData>> func)
                {
                    var attrs = func(prop, properties);

                    if (attrs != null && attrs.Any())
                    {
                        foreach (var attr in attrs)
                        {
                            var existing = attributes.SingleOrDefault(x => x.Name == attr.Name);
                            if (existing != null)
                            {
                                existing.Params = existing.Params
                                    .Concat(attr.Params)
                                    .GroupBy(x => x.Key)
                                    .Select(x => x.First())
                                    .ToList();

                                continue;
                            }

                            attributes.Add(attr);
                        }
                    }
                }

                MaybeAddAttr(Unicode);
                MaybeAddAttr(Key);
                MaybeAddAttr(StringLength);
                MaybeAddAttr(Computed);
                MaybeAddAttr(Identity);
                MaybeAddAttr(DbNone);
                MaybeAddAttr(Required);
                MaybeAddAttr(Rename);
                MaybeAddAttr(DataType);

                return attributes;
            }

            private static ICollection<AttrMetaData> Rename(TableProperty prop, ICollection<TableProperty> properties)
            {
                var sanitizedName = Shared.NameSanitize(prop.ColumnName);

                if (prop.ColumnName.Length == sanitizedName.Length)
                {
                    return null;
                }

                var attr = new AttrMetaData { Name = "Column" };
                attr.Params.Add(new KeyValuePair<string, string>("", $"\"{prop.ColumnName}\""));

                return new[] { attr };
            }

            private static readonly HashSet<string> StringTypes = new HashSet<string>
            {
                "char"
                , "varchar"
                , "nchar"
                , "nvarchar"
                , "varbinary"
            };


            private static readonly HashSet<string> NumericTypes = new HashSet<string>
            {
                "numeric"
                , "decimal"
            };

            private static ICollection<AttrMetaData> DataType(TableProperty prop, ICollection<TableProperty> properties)
            {
                if (StringTypes.Contains(prop.DataType))
                {
                    var attr = new AttrMetaData { Name = "Column" };
                    attr.Params.Add(new KeyValuePair<string, string>("TypeName", $"\"{prop.DataType}({(prop.Size > -1 ? prop.Size.ToString() : "MAX")})\""));
                    return new[] { attr };
                }


                if (NumericTypes.Contains(prop.DataType))
                {
                    var attr = new AttrMetaData { Name = "Column" };
                    attr.Params.Add(new KeyValuePair<string, string>("TypeName", $"\"{prop.DataType}({prop.Precision}, {prop.Scale})\""));
                    return new[] { attr };
                }

                return null;
            }

            private static ICollection<AttrMetaData> Required(TableProperty prop, ICollection<TableProperty> properties)
            {
                if (prop.IsNullable || !Shared.NullableMap.Contains(prop.DataType))
                {
                    return null;
                }

                return new[] { new AttrMetaData { Name = "Required" } };
            }

            private static ICollection<AttrMetaData> StringLength(TableProperty prop, ICollection<TableProperty> properties)
            {
                if (prop.Size == null || prop.Size == -1)
                {
                    return null;
                }

                var attr = new AttrMetaData { Name = "StringLength" };
                attr.Params.Add(new KeyValuePair<string, string>("", prop.Size.ToString()));

                if (MinLengthTypes.Contains(prop.DataType))
                {
                    attr.Params.Add(new KeyValuePair<string, string>("MinimumLength", prop.Size.ToString()));
                }

                return new[] { attr }; ;
            }

            private static ICollection<AttrMetaData> Computed(TableProperty prop, ICollection<TableProperty> properties)
            {
                if (!prop.IsComputed)
                {
                    return null;
                }

                var attr = new AttrMetaData { Name = "DatabaseGenerated" };
                attr.Params.Add(new KeyValuePair<string, string>("", "DatabaseGeneratedOption.Computed"));

                return new[] { attr }; ;
            }

            private static ICollection<AttrMetaData> Identity(TableProperty prop, ICollection<TableProperty> properties)
            {
                if (!prop.IsIdentity || prop.IsKey)
                {
                    return null;
                }

                var attr = new AttrMetaData { Name = "DatabaseGenerated" };
                attr.Params.Add(new KeyValuePair<string, string>("", "DatabaseGeneratedOption.Identity"));

                return new[] { attr }; ;
            }

            private static ICollection<AttrMetaData> DbNone(TableProperty prop, ICollection<TableProperty> properties)
            {
                if (!prop.IsKey || prop.IsIdentity)
                {
                    return null;
                }

                // composite PK not db generated is implied
                if (properties.Count(x => x.IsKey) > 1)
                {
                    return null;
                }

                var attr = new AttrMetaData { Name = "DatabaseGenerated" };
                attr.Params.Add(new KeyValuePair<string, string>("", "DatabaseGeneratedOption.None"));

                return new[] { attr }; ;
            }

            private static ICollection<AttrMetaData> Key(TableProperty prop, ICollection<TableProperty> properties)
            {
                if (!prop.IsKey)
                {
                    return null;
                }

                var attrs = new List<AttrMetaData> { new AttrMetaData { Name = "Key" } };

                if (properties.Count(x => x.IsKey) > 1)
                {
                    var order = -1;
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var tableProperty = properties.ElementAt(i);
                        if (tableProperty.IsKey)
                        {
                            order++;

                            if (tableProperty.ColumnName == prop.ColumnName)
                            {
                                var columnAttr = new AttrMetaData { Name = "Column" };
                                columnAttr.Params.Add(new KeyValuePair<string, string>("Order", order.ToString()));
                                attrs.Add(columnAttr);
                            }
                        }
                    }
                }

                return attrs;
            }

            private static ICollection<AttrMetaData> Unicode(TableProperty prop, ICollection<TableProperty> properties)
            {
                if (!UnicodeTypes.Contains(prop.DataType))
                {
                    return null;
                }

                return new[] { new AttrMetaData { Name = "IsUnicode" } };
            }
        }

        private class AttrMetaData
        {
            public string Name { get; set; }
            public ICollection<KeyValuePair<string, string>> Params { get; set; } = new HashSet<KeyValuePair<string, string>>();
        }
    }
}
