namespace JustyBase.Editor;

public static class ProeprtyExtensions
{
    public static bool Has(this PropertyOptions options, PropertyOptions value) =>
        (options & value) == value;
}