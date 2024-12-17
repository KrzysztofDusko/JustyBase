using System.Text.Json.Serialization;

namespace JustyBase.PluginCommon.Models;

public sealed class LoginDataModel
{
    public required string ConnectionName { get; set; }
    public required string Driver { get; set; }
    public string? Server { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? Database { get; set; }
    public int? DefaultIndex { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented =true)]
[JsonSerializable(typeof(List<LoginDataModel>))]
public partial class MyJsonContextLoginDataModelList : JsonSerializerContext
{
}

[JsonSerializable(typeof(LoginDataModel))]
public partial class MyJsonContextLoginDataModel : JsonSerializerContext
{
}