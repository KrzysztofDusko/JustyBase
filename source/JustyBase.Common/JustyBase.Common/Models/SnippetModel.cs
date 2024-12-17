using System.Collections.Generic;

namespace JustyBase.Common.Models;

public sealed class SnippetModel
{
    public string? SnippetType { get; set; }
    public string? SnippetName { get; set; }
    public string? SnippetDesc { get; set; }
    public string? SnippetText { get; set; }

    public List<string> TEXT_TYPES { get; init; } = [STANDARD_STRING, FAST_STRING, TYPO_STRING];

    public const string STANDARD_STRING = AppOptions.STANDARD_SNIPET_TXT;
    public const string FAST_STRING = AppOptions.FAST_SNIPET_TXT;
    public const string TYPO_STRING = AppOptions.TYPO_SNIPET_TXT;

    //private const string KEYWORDS_STRING = "keywords";
}