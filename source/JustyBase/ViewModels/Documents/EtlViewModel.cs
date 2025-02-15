namespace JustyBase.ViewModels.Documents;
public class EtlViewModel : DocumentBaseVM
{
    private static EtlViewModel _instance;
    public static EtlViewModel Instance => _instance ??= new EtlViewModel();

    public string EtlMsg { get; set; }
    private EtlViewModel()
    {
        Title = "Etl document - TO DO";
        EtlMsg = "xyz";
    }

}

