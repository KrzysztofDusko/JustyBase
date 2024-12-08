using JustyBase.PluginDatabaseBase.Enums;

namespace JustyBase.PluginDatabaseBase.Models;

public record DatabaseObject(int Id, string Name, string? Desc, TypeInDatabaseEnum TypeInDatabase, string TextType, string Owner, DateTime? CreateDateTime);
