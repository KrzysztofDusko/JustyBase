namespace JustyBase.PluginDatabaseBase.Models;

public record PorcedureCachedInfo
{
    public int Id { get; set; }
    public string ProcedureSource { get; set; }
    public bool ExecuteAsOwner { get; set; }
    public string ProcedureSignature { get; set; }
    public string Arguments { get; set; }
    public string Returns { get; set; }
    public string Desc { get; set; }
    public string ProcLanguage { get; set; }
    //public string SPECIFICNAME { get; set; }
}