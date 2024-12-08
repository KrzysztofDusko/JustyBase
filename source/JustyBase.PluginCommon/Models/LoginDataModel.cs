using System.Text.Json.Serialization;

namespace PluginDatabaseBase.Models;

public sealed class LoginDataModel
{
    public string? ConnectionName { get; set; }
    public string? Driver { get; set; }
    public string? Server { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? Database { get; set; }
    public int? DefaultIndex { get; set; }
}

[JsonSerializable(typeof(List<LoginDataModel>))]
public partial class MyJsonContextLoginDataModelList : JsonSerializerContext
{
}

[JsonSerializable(typeof(LoginDataModel))]
public partial class MyJsonContextLoginDataModel : JsonSerializerContext
{
}