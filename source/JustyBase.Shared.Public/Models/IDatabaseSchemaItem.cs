using JustyBase.Common.Contracts;
using JustyBase.Editor;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommon.Models;
using JustyBase.PluginDatabaseBase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustyBase.Services.Database;

internal interface IDatabaseSchemaItem
{
    string Database { get; set; }
    string CurrentSchema { get; set; }
    string Name { get; set; }
    TypeInDatabaseEnum ActualTypeInDatabase { get; set; }

    internal static void InsertDoubleClicked(IDatabaseSchemaItem schemaModel)
    {
        var editor = SqlCodeEditorHelpers.LastFocusedEditor;
        if (editor is null)
        {
            return;
        }
        string textToInsert = schemaModel.Name;
        if (schemaModel.ActualTypeInDatabase == TypeInDatabaseEnum.Table)
        {
            textToInsert = $"{schemaModel.Database}.{schemaModel.CurrentSchema}.{schemaModel.Name}";
        }
        else if (schemaModel.ActualTypeInDatabase == TypeInDatabaseEnum.View)
        {
            textToInsert = $"{schemaModel.Database}.{schemaModel.CurrentSchema}.{schemaModel.Name}";
        }

        editor.Document.Insert(editor.TextArea.Caret.Offset, textToInsert);
        editor.Focus();
    }

    internal static async Task<string> GetCode(IDatabaseSchemaItem schemaModel, string CONNECTION_NAME, string optionName)
    {
        string DATABASE = schemaModel.Database;
        string SCHEMA = schemaModel.CurrentSchema;
        string ITEM_NAME = schemaModel.Name;
        IGeneralApplicationData generalApplicationData = App.GetRequiredService<IGeneralApplicationData>();
        ISimpleLogger simpleLogger = App.GetRequiredService<ISimpleLogger>();
        IDatabaseService dbService = DatabaseServiceHelpers.GetDatabaseService(generalApplicationData, CONNECTION_NAME);
        string sql = "";
        if (optionName.StartsWith("DDL_TABLE"))
        {
            sql = await dbService.GetCreateTableText(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("RECREATE_TABLE"))
        {
            sql = await dbService.GetReCreateTableText(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("RECREATE_ALL_TABLES"))
        {
            HashSet<string> words = null;
            try
            {
                if (SqlCodeEditorHelpers.LastFocusedEditor is not null && SqlCodeEditorHelpers.LastFocusedEditor.Text.StartsWith("RECREATE_HACK"))
                {
                    words = new HashSet<string>(SqlCodeEditorHelpers.LastFocusedEditor.Text.Split("\n").Select(o => o.Trim()));
                }
            }
            catch (Exception ex)
            {
                simpleLogger.TrackError(ex, isCrash: false);
            }
            var objects = dbService.GetDbObjects(DATABASE, SCHEMA, "", TypeInDatabaseEnum.Table);
            StringBuilder stringBuilder = new();
            foreach (var item in objects)
            {
                if (words is null || words.Contains(item.Name))
                {
                    await dbService.GetReCreateTableTextStringBuilder(stringBuilder, DATABASE, SCHEMA, item.Name);
                }
            }
            sql = stringBuilder.ToString();
        }
        else if (optionName.StartsWith("DDL_ALL_TABLES"))
        {
            var objects = dbService.GetDbObjects(DATABASE, SCHEMA, "", TypeInDatabaseEnum.Table);
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in objects)
            {
                await dbService.GetCreateTableTextStringBuilder(stringBuilder, DATABASE, SCHEMA, item.Name);
            }
            sql = stringBuilder.ToString();
        }
        else if (optionName.StartsWith("SELECT_ALL_SEARCH_TEXT"))
        {
            IEnumerable<DatabaseObject> objects = dbService.GetDbObjects(DATABASE, SCHEMA, "", TypeInDatabaseEnum.Table);
            sql = dbService.GetTop100SelectTextFromTables(DATABASE, SCHEMA, objects);
        }
        else if (optionName.StartsWith("SELECT_ALL_SEARCH_NUMBER"))
        {
            IEnumerable<DatabaseObject> objects = dbService.GetDbObjects(DATABASE, SCHEMA, "", TypeInDatabaseEnum.Table);
            sql = dbService.GetTop100SelectNumberFromTables(DATABASE, SCHEMA, objects);
        }
        else if (optionName.StartsWith("SELECT_SEARCH"))
        {
            sql = dbService.GetTop100Select(DATABASE, SCHEMA, ITEM_NAME, snippetMode: false /*!!*/, addWhereToTextCols: true);
        }
        else if (optionName.StartsWith("SELECT"))
        {
            if (optionName.EndsWith("CLIP"))
            {
                sql = dbService.GetTop100Select(DATABASE, SCHEMA, ITEM_NAME, snippetMode: false);
            }
            else
            {
                sql = dbService.GetTop100Select(DATABASE, SCHEMA, ITEM_NAME, snippetMode: true);
            }
        }
        else if (optionName.StartsWith("DUPLICATES"))
        {
            sql = dbService.GetDuplicates(ITEM_NAME, DATABASE, SCHEMA);
        }
        else if (optionName.StartsWith("DELETED"))
        {
            sql = dbService.GetDeleted(ITEM_NAME, DATABASE, SCHEMA);
        }
        else if (optionName.StartsWith("GRANT"))
        {
            sql = dbService.GetGrant(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("ORGANIZE"))
        {
            sql = dbService.GetOrganize(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("DISTRIBUTE"))
        {
            sql = dbService.GetCheckDistributeText(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("DDL_VIEW"))
        {
            sql = await dbService.GetCreateViewText(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("DDL_ALL_VIEWS"))
        {
            var objects = dbService.GetDbObjects(DATABASE, SCHEMA, "", TypeInDatabaseEnum.View);
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in objects)
            {
                await dbService.GetCreateViewTextStringBuilder(stringBuilder, DATABASE, SCHEMA, item.Name);
            }
            sql = stringBuilder.ToString();
        }
        else if (optionName.StartsWith("SELECT_VIEW"))
        {
            sql = dbService.GetTop100Select(DATABASE, SCHEMA, ITEM_NAME, snippetMode: true);
        }
        else if (optionName.StartsWith("DDL_PROCEDURE"))
        {
            sql = await dbService.GetCreateProcedureText(DATABASE, SCHEMA, ITEM_NAME, forceFreshCode: true);
        }
        else if (optionName.StartsWith("DDL_ALL_PROCEDURES"))
        {
            var objects = dbService.GetDbObjects(DATABASE, SCHEMA, "", TypeInDatabaseEnum.Procedure);
            StringBuilder stringBuilder = new();
            foreach (var item in objects)
            {
                await dbService.GetCreateProcedureTextStringBuilder(stringBuilder, DATABASE, SCHEMA, item.Name);
            }
            sql = stringBuilder.ToString();
        }
        else if (optionName.StartsWith("CALL_PROCEDURE"))
        {
            sql = dbService.GetCreateProcedureCall(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("CREATE_PROCEDURE"))
        {
            sql = dbService.GetCreateProcedurePatternText();
        }
        else if (optionName.StartsWith("FLUID_SAMPLE") && dbService is INetezza netezza)
        {
            sql = netezza.GetCreateFluidSample(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("KEY"))
        {
            sql = dbService.GetKeyCodeText(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("UNIQUE"))
        {
            sql = dbService.GetKeyUiqueCodeText(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("DDL_EXTERNAL"))
        {
            sql = await dbService.GetCreateExternalText(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("DDL_ALL_EXTERNALS"))
        {
            var objects = dbService.GetDbObjects(DATABASE, SCHEMA, "", TypeInDatabaseEnum.ExternalTable);
            StringBuilder stringBuilder = new();
            foreach (var item in objects)
            {
                await dbService.GetCreateExternalTextStringBuilder(stringBuilder, DATABASE, SCHEMA, item.Name);
            }
            sql = stringBuilder.ToString();
        }
        else if (optionName.StartsWith("DDL_SYNONYM"))
        {
            sql = await dbService.GetCreateSynonymText(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("COPY_TEXT"))
        {
            sql = ITEM_NAME;
        }
        else if (optionName.StartsWith("DDL_ALL_SYNONYMS"))
        {
            var objects = dbService.GetDbObjects(DATABASE, SCHEMA, "", TypeInDatabaseEnum.Synonym);
            StringBuilder stringBuilder = new();
            foreach (var item in objects)
            {
                await dbService.GetCreateSynonymTextStringBuilder(stringBuilder, DATABASE, SCHEMA, item.Name);
            }
            sql = stringBuilder.ToString();
        }
        else if (optionName.StartsWith("CREATE_SYNONYM"))
        {
            sql = dbService.GetCreateSynonymPatternText();
        }
        else if (optionName.StartsWith("CREATE_SEQUENCE"))
        {
            sql = dbService.GetCreateSequencePatternText();
        }
        else if (optionName.StartsWith("GROOM"))
        {
            sql = dbService.GetGroom(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("STATS"))
        {
            sql = dbService.GetGenerateStats(DATABASE, SCHEMA, ITEM_NAME);
        }
        else if (optionName.StartsWith("COMMENT"))
        {
            sql = dbService.GetAddComment(ITEM_NAME, DATABASE, SCHEMA);
        }
        else if (optionName.StartsWith("DROP"))
        {
            sql = dbService.GetDrop(ITEM_NAME, DATABASE, SCHEMA);
        }
        else if (optionName.StartsWith("EMPTY"))
        {
            sql = dbService.GetEmpty(ITEM_NAME, DATABASE, SCHEMA);
        }
        else if (optionName.StartsWith("EXPORT_DATA"))
        {
            sql = dbService.GetExport(ITEM_NAME, DATABASE, SCHEMA);
        }
        else if (optionName.StartsWith("IMPORT_DATA"))
        {
            sql = dbService.GetImport(ITEM_NAME, DATABASE, SCHEMA);
        }

        return sql;

    }

}