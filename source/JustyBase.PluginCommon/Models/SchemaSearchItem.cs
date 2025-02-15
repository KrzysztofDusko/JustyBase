namespace JustyBase.PluginCommon.Models;

public record class SchemaSearchItem
{
    public int Id { get; init; }
    public required string Type { get; init; }
    public required string Name { get; init; }
    public required string Db { get; init; }
    public string Desc { get; init; }
    public string Schema { get; init; }
    public string Owner { get; init; }
    public DateTime? CreationDateTime { get; init; }
    public string MoreInfo { get; init; }
    public string ParentType { get; init; }
    public string ParentName { get; init; }
    public bool FilterNotOk { get; set; }

    //    public override string ToString()
    //    {
    //        return $@"Type:{Type} 
    //Name: {Name}
    //Db: {Db}
    //Desc: {Desc}
    //Schema: {Schema}
    //";
    //    }


    public string[] GetPath(string connectionName)
    {
        string properItemName = Type + "s";
        string parentTypeName = ParentType + "s";
        string[] toExpandPath;

        if (Type == "Fluid")
        {
            properItemName = "Fluid Query Data Sources";
        }

        if (Type == "Column" && parentTypeName == "Views")
        {
            toExpandPath = [connectionName, Db, Schema, parentTypeName, ParentName, Name];
        }
        else if (Type == "Column")
        {
            toExpandPath = [connectionName, Db, Schema, parentTypeName, ParentName, properItemName, Name];
        }
        //else if (Type == "Procedure")
        //{
        //    int ind = Name.IndexOf("(");
        //    string name = Name;
        //    if (ind > 0)
        //    {
        //        name = Name[0..ind];
        //    }
        //    toExpandPath = new string[] { connectionName, Db, Schema, properItemName, name };
        //}
        else
        {
            toExpandPath = [connectionName, Db, Schema, properItemName, Name];
        }
        return toExpandPath;
    }
}
