using JustyBase.Editor;
using JustyBase.Editor.CompletionProviders;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommons;
using System;
using System.Collections.Generic;
using System.Text;

namespace JustyBase.Helpers;

public sealed class AutocompleteService
{
    public IEnumerable<CompletionDataSql> GetWordsList(string input, Dictionary<string, List<string>> aliasDbTable,
        Dictionary<string, List<string>> subqueryHints,Dictionary<string, List<string>> withHints,
        Dictionary<string, List<string>> tempTableHints, IDatabaseService databaseService, string? databaseName
    )
    {
        input.GetDotsPositionsAndCount(out int lastDotIndex, out int howManyDots,out int firsDotIndex);
        if (howManyDots == 0 && input.Length <= 2)
        {
            yield break;
        }

        string firstWord = "";
        string middleWord = "";
        string lastWord = "";
        if (databaseService is null)
        {
            yield break;
        }
        bool TYPE3_DB_SCHEMA_OBJECT = ((databaseService.AutoCompletDatabaseMode & CurrentAutoCompletDatabaseMode.DatabaseSchemaTable) != CurrentAutoCompletDatabaseMode.NotSet);// DB.SCHEMA.TABLE type
        bool TYPE2_SCHEMA_OBJECT = ((databaseService.AutoCompletDatabaseMode & CurrentAutoCompletDatabaseMode.SchemaTable) != CurrentAutoCompletDatabaseMode.NotSet);// SCHEMA.TABLE type
        bool TYPE_SCHEMA_OPTIONAL = ((databaseService.AutoCompletDatabaseMode & CurrentAutoCompletDatabaseMode.DatabaseAndSchemaOptional) != CurrentAutoCompletDatabaseMode.NotSet);
        bool TYPE_SCHEMA_CAN_BE_NULL = ((databaseService.AutoCompletDatabaseMode & CurrentAutoCompletDatabaseMode.NullSchemaCanBeAccepted) != CurrentAutoCompletDatabaseMode.NotSet);

        string? TEMP_DB = null;
        if (TYPE3_DB_SCHEMA_OBJECT)
        {
            TEMP_DB = databaseName;
        }

        if (firsDotIndex != -1)
        {
            firstWord = input[..firsDotIndex];
        }

        if (lastDotIndex != firsDotIndex)
        {
            middleWord = input[(firsDotIndex + 1)..lastDotIndex];
            lastWord = input[(lastDotIndex + 1)..];
        }
        else
        {
            lastWord = input[(firsDotIndex + 1)..];
        }

        if (howManyDots == 0 && lastWord.Length > 0)
        {
            foreach (var item in subqueryHints.Keys)
            {
                if (item.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new CompletionDataSql(item, "subquery", false, Glyph.SubQuery, null);
                }
            }

            foreach (var item in aliasDbTable.Values)
            {
                foreach (var item2 in item)
                {
                    if (item2.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return new CompletionDataSql(item2, "alias", false, Glyph.Table, null);
                    }
                }
            }

            foreach (var item in withHints.Keys)
            {

                if (item.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new CompletionDataSql(item, "with", false, Glyph.WithDb, null);
                }
            }

            foreach (var item in tempTableHints.Keys)
            {
                if (item.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new CompletionDataSql(item, "tempTable", false, Glyph.TempTable, null);
                }
            }

            foreach (var columnAutocomplete in getColumnAutocomplete(lastWord))
            {
                yield return columnAutocomplete;
            }
        }


        if (TYPE3_DB_SCHEMA_OBJECT && howManyDots == 0 && lastWord.Length > 0)
        {
            foreach (string item in databaseService.GetDatabases(lastWord))
            {
                yield return new CompletionDataSql(item, "database", false, Glyph.Database, null);
            }

            if (TYPE_SCHEMA_OPTIONAL && lastWord.Length >= 3)
            {
                foreach (string schemaName in databaseService.GetSchemas(TEMP_DB, ""))
                {
                    foreach (var itme in getSchemaObjectsForAutocomplete(TEMP_DB, schemaName, lastWord))
                    {
                        yield return itme;
                    }
                }
            }
        }

        if (TYPE2_SCHEMA_OBJECT && howManyDots == 0) // schema autocomplete
        {
            foreach (string item in databaseService.GetSchemas(TEMP_DB, lastWord))
            {
                yield return new CompletionDataSql(item, "schema", false, Glyph.Schema, null);
            }
        }

        if (howManyDots == 1)
        {
            if (subqueryHints.TryGetValue(firstWord, out var strings))
            {
                foreach (var item in strings)
                {
                    if (item.Contains(lastWord, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return new CompletionDataSql(item, "subquert column", false, Glyph.None, null);
                    }
                }
            }
            if (withHints.TryGetValue(firstWord, out var strings2))
            {
                foreach (var item in strings2)
                {
                    if (item.Contains(lastWord, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return new CompletionDataSql(item, "temp table column", false, Glyph.None, null);
                    }
                }
            }
            if (tempTableHints.TryGetValue(firstWord, out var strings3))
            {
                foreach (var item in strings3)
                {
                    if (item.Contains(lastWord, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return new CompletionDataSql(item, "temp table column", false, Glyph.Table, null);
                    }
                }
            }

            foreach (KeyValuePair<string, List<string>> longName in aliasDbTable)
            {
                var parts = longName.Key.Split('.');
                foreach (string alias in longName.Value)
                {
                    if (parts.Length == 1 && firstWord.Equals(alias, StringComparison.OrdinalIgnoreCase) && withHints.TryGetValue(longName.Key, out var colList))
                    {
                        foreach (var item in colList)
                        {
                            yield return new CompletionDataSql(item, "with column", false, Glyph.None, null);
                        }
                    }
                    else if (parts.Length == 1 && firstWord.Equals(alias, StringComparison.OrdinalIgnoreCase) && tempTableHints.TryGetValue(longName.Key, out var colList2))
                    {
                        foreach (var item in colList2)
                        {
                            yield return new CompletionDataSql(item, "temp table column", false, Glyph.None, null);
                        }
                    }
                }
            }
        }

        if (TYPE2_SCHEMA_OBJECT && howManyDots == 1)
        {
            foreach (var item in getSchemaObjectsForAutocomplete(TEMP_DB, firstWord, lastWord))
            {
                yield return item;
            }
        }

        if (TYPE3_DB_SCHEMA_OBJECT && howManyDots == 1)
        {
            foreach (string item in databaseService.GetSchemas(firstWord, lastWord))
            {
                yield return new CompletionDataSql(item, "schema", false, Glyph.Schema, null);
            }
        }

        if ((TYPE3_DB_SCHEMA_OBJECT || TYPE2_SCHEMA_OBJECT) && howManyDots == 1)
        {
            foreach (var longName in aliasDbTable)
            {
                var parts = longName.Key.Split('.');
                foreach (string alias in longName.Value)
                {
                    if (parts.Length == 3
                        || parts.Length == 2 && TYPE2_SCHEMA_OBJECT
                        || parts.Length == 1 && TYPE_SCHEMA_OPTIONAL
                        )
                    {
                        if (firstWord.Equals(alias, StringComparison.OrdinalIgnoreCase))
                        {
                            string? DB1;
                            string? SCH1;
                            string OBJ1;
                            if (parts.Length == 3)
                            {
                                (DB1, SCH1, OBJ1) = (parts[0], parts[1], parts[2]);
                            }
                            else if (parts.Length == 2)
                            {
                                (DB1, SCH1, OBJ1) = (TEMP_DB, parts[0], parts[1]);
                            }
                            else
                            {
                                (DB1, SCH1, OBJ1) = (TEMP_DB, null, parts[0]);
                            }
                            //handle "word", WORD, word, Word etc. in netezza.
                            DB1 = databaseService.CleanSqlWord(DB1, databaseService.AutoCompletDatabaseMode);
                            SCH1 = databaseService.CleanSqlWord(SCH1, databaseService.AutoCompletDatabaseMode);
                            OBJ1 = databaseService.CleanSqlWord(OBJ1, databaseService.AutoCompletDatabaseMode);

                            foreach (var item in databaseService.GetColumns(DB1, SCH1, OBJ1, lastWord))
                            {
                                yield return new CompletionDataSql(item.Name, $"{item.FullTypeName}\n{item.Desc}", false, Glyph.Column, null);
                            }
                        }
                    }
                }
            }
        }

        if (TYPE3_DB_SCHEMA_OBJECT && howManyDots == 2 &&
            (!string.IsNullOrEmpty(middleWord) || TYPE_SCHEMA_CAN_BE_NULL))
        {
            foreach (var item in getSchemaObjectsForAutocomplete(firstWord, middleWord, lastWord))
            {
                yield return item;
            }
        }

        IEnumerable<CompletionDataSql> getSchemaObjectsForAutocomplete(string firstWord, string middleWord, string lastWord)
        {
            var listOfTypes = new TypeInDatabaseEnum[] { TypeInDatabaseEnum.Table, TypeInDatabaseEnum.View, TypeInDatabaseEnum.Synonym, TypeInDatabaseEnum.Procedure, TypeInDatabaseEnum.ExternalTable };
            foreach (var type in listOfTypes)
            {
                foreach (var item in databaseService.GetDbObjects(firstWord, middleWord, lastWord, type))
                {
                    Glyph g = item.TypeInDatabase switch
                    {
                        TypeInDatabaseEnum.Table => Glyph.Table,
                        TypeInDatabaseEnum.View => Glyph.View,
                        TypeInDatabaseEnum.Procedure => Glyph.Procedure,
                        TypeInDatabaseEnum.Synonym => Glyph.Synonym,
                        _ => Glyph.None
                    };

                    yield return new CompletionDataSql(item.Name, prepareDesc(item.Desc), false, g, null);
                }
            }
        }

        string prepareDesc(string? descProposal)
        {
            if (descProposal is null)
            {
                return "no object desc";
            }
            if (descProposal.Length >= 2_048)
            {
                StringBuilder sb = new(2_048);
                sb.Append(descProposal.AsSpan()[..^3]);
                sb.Append("...");
                return sb.ToString();
            }
            return descProposal;
        }

        IEnumerable<CompletionDataSql> getColumnAutocomplete(string lastWord)
        {
            foreach (var item in aliasDbTable.Keys)
            {
                var parts = item.Split('.');
                string? partDatabase = null;
                string? partSchema = null;
                string? partObject = null;
                if (parts.Length == 1 && TYPE_SCHEMA_OPTIONAL)
                {
                    partDatabase = TEMP_DB;
                    partSchema = null;
                    partObject = parts[0];
                }
                else if (parts.Length == 2)
                {
                    partDatabase = TEMP_DB;
                    partSchema = parts[0];
                    partObject = parts[1];
                }
                else if (parts.Length == 3)
                {
                    partDatabase = parts[0];
                    partSchema = parts[1];
                    partObject = parts[2];
                }

                foreach (var alias in aliasDbTable[item])
                {
                    var alias2 = alias == "" ? partObject : alias;
                    foreach (var item2 in databaseService.GetColumns(partDatabase, partSchema, partObject, lastWord))
                    {
                        yield return new CompletionDataSql(alias2 + "." + item2.Name, $"{item2.FullTypeName}\n{item2.Desc}", false, Glyph.Column, null);
                    }
                }
            }
        }

    }

}

