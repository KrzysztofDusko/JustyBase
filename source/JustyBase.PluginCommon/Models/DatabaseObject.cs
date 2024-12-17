using JustyBase.PluginCommon.Enums;

namespace JustyBase.PluginCommon.Models;

public record DatabaseObject(int Id, string Name, string? Desc, TypeInDatabaseEnum TypeInDatabase, string TextType, string Owner, DateTime? CreateDateTime);
