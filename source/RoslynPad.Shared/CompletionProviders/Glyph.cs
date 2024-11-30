namespace JustyBase.Editor.CompletionProviders;

public enum Glyph
{
    None,
    Snippet,
    Table,
    Column,
    CompletionWarning,
    Database,
    SubQuery,
    WithDb,
    TempTable,
    BetweenSelectAndFrom,
    Schema,
    View,
    Procedure,
    Synonym,
    ExternalTable
}

public static class GlyphExtensions
{
    public static CommonImage? TableBitmap { get; set; }
    public static CommonImage? ColumnBitmap { get; set; }
    public static CommonImage? ViewBitmap { get; set; }
    public static CommonImage? DatabaseBitmap { get; set; }
    public static CommonImage? ProcedureBitmap { get; set; }
    public static CommonImage? SynonymBitmap { get; set; }
    public static CommonImage? SchemaBitmap { get; set; }
    public static CommonImage? ExternalBitmap { get; set; }

    public static CommonImage? ToImageSource(this Glyph glyph) => glyph switch
    {
        Glyph.Snippet => null,
        Glyph.Table => TableBitmap,
        Glyph.Column => ColumnBitmap,
        Glyph.View => ViewBitmap,
        Glyph.Database => DatabaseBitmap,
        Glyph.Procedure => ProcedureBitmap,
        Glyph.Synonym => SynonymBitmap,
        Glyph.Schema => SchemaBitmap,
        Glyph.ExternalTable => ExternalBitmap,
        _ => null,
    };
}

