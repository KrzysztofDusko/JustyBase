﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JustyBase.Editor;

public sealed partial class CodeSnippet(string name, string description, string text, string keyword)
{
    public string Name { get; } = name;

    public string Text { get; } = text;

    public string Description { get; } = description;

    public bool HasSelection
    {
        get
        {
            return s_pattern.Matches(Text)
                .OfType<Match>()
                .Any(item => item.Value == "${Selection}");
        }
    }

    public string Keyword { get; } = keyword;

    public Snippet CreateAvalonEditSnippet()
    {
        return CreateAvalonEditSnippet(Text);
    }

    private static readonly Regex s_pattern = MyRegex();

    public static Snippet CreateAvalonEditSnippet(string snippetText)
    {
        ArgumentNullException.ThrowIfNull(snippetText);
        var replaceableElements = new Dictionary<string, SnippetReplaceableTextElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var match in s_pattern.Matches(snippetText).OfType<Match>())
        {
            var val = match.Groups[1].Value;
            var equalsSign = val.IndexOf('=');
            if (equalsSign > 0)
            {
                var name = val.Substring(0, equalsSign);
                replaceableElements[name] = new SnippetReplaceableTextElement();
            }
        }
        var snippet = new Snippet();
        var pos = 0;
        foreach (var match in s_pattern.Matches(snippetText).OfType<Match>())
        {
            if (pos < match.Index)
            {
                snippet.Elements.Add(new SnippetTextElement { Text = snippetText.Substring(pos, match.Index - pos) });
            }
            snippet.Elements.Add(CreateElementForValue(replaceableElements, match.Groups[1].Value, match.Index, snippetText));
            pos = match.Index + match.Length;
        }
        if (pos < snippetText.Length)
        {
            snippet.Elements.Add(new SnippetTextElement { Text = snippetText.Substring(pos) });
        }
        if (!snippet.Elements.Any(e => e is SnippetCaretElement))
        {
            var element = snippet.Elements.Select((s, i) => new { s, i }).FirstOrDefault(s => s.s is SnippetSelectionElement);
            var index = element?.i ?? -1;
            if (index > -1)
                snippet.Elements.Insert(index + 1, new SnippetCaretElement());
        }
        return snippet;
    }

    private static readonly Regex s_functionPattern = FunctionPattern();

    private static SnippetElement CreateElementForValue(Dictionary<string, SnippetReplaceableTextElement> replaceableElements, string val, int offset, string snippetText)
    {
        SnippetReplaceableTextElement? srte;
        var equalsSign = val.IndexOf('=');
        if (equalsSign > 0)
        {
            var name = val.Substring(0, equalsSign);
            if (replaceableElements.TryGetValue(name, out srte))
            {
                srte.Text ??= val.Substring(equalsSign + 1);
                return srte;
            }
        }

        var element = GetDefaultElement(val, snippetText, offset);
        if (element != null)
        {
            return element;
        }

        if (replaceableElements.TryGetValue(val, out srte))
        {
            return new SnippetBoundElement { TargetElement = srte };
        }
        var m = s_functionPattern.Match(val);
        if (m.Success)
        {
            var f = GetFunction(m.Groups[1].Value);
            if (f != null)
            {
                var innerVal = m.Groups[2].Value;
                if (replaceableElements.TryGetValue(innerVal, out srte))
                {
                    return new FunctionBoundElement { TargetElement = srte, Function = f };
                }
                return new SnippetTextElement { Text = f(innerVal) };
            }
        }

        return new SnippetReplaceableTextElement { Text = val }; // ${unknown} -> replaceable element
    }
    
    private static SnippetElement? GetDefaultElement(string tag, string snippetText, int position)
    {
        if ("Selection".Equals(tag, StringComparison.OrdinalIgnoreCase))
        {
            return new SnippetSelectionElement { Indentation = GetWhitespaceBefore(snippetText, position).Length };
        }

        if ("Caret".Equals(tag, StringComparison.OrdinalIgnoreCase))
        {
            // If a ${Selection} exists, use the ${Caret} only if there is text selected
            // (if no text is selected, ${Selection} will set the caret
            return snippetText.Contains("${Selection}", StringComparison.OrdinalIgnoreCase)
                ? new SnippetCaretElement(setCaretOnlyIfTextIsSelected: true)
                : new SnippetCaretElement();
        }

        return null;
    }

    private static readonly char[] _newLiness = ['\r', '\n'];
    private static string GetWhitespaceBefore(string snippetText, int offset)
    {
        var start = snippetText.LastIndexOfAny(_newLiness, offset) + 1;
        return snippetText.Substring(start, offset - start);
    }

    private static Func<string, string>? GetFunction(string name)
    {
        if ("toLower".Equals(name, StringComparison.OrdinalIgnoreCase))
            return s => s.ToLower();
        if ("toUpper".Equals(name, StringComparison.OrdinalIgnoreCase))
            return s => s.ToUpper();
        if ("toFieldName".Equals(name, StringComparison.OrdinalIgnoreCase))
            return GetFieldName;
        if ("toPropertyName".Equals(name, StringComparison.OrdinalIgnoreCase))
            return GetPropertyName;
        if ("toParameterName".Equals(name, StringComparison.OrdinalIgnoreCase))
            return GetParameterName;
        return null;
    }

    private static string GetPropertyName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
            return fieldName;
        if (fieldName.StartsWith('_') && fieldName.Length > 1)
            return char.ToUpper(fieldName[1]) + fieldName.Substring(2);
        if (fieldName.StartsWith("m_") && fieldName.Length > 2)
            return char.ToUpper(fieldName[2]) + fieldName.Substring(3);
        return char.ToUpper(fieldName[0]) + fieldName.Substring(1);
    }

    private static string GetParameterName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
            return fieldName;
        if (fieldName.StartsWith('_') && fieldName.Length > 1)
            return char.ToLower(fieldName[1]) + fieldName.Substring(2);
        if (fieldName.StartsWith("m_") && fieldName.Length > 2)
            return char.ToLower(fieldName[2]) + fieldName.Substring(3);
        return char.ToLower(fieldName[0]) + fieldName.Substring(1);
    }

    private static string GetFieldName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;
        var newName = char.ToLower(propertyName[0]) + propertyName.Substring(1);
        if (newName == propertyName)
            return "_" + newName;
        return newName;
    }

    private sealed class FunctionBoundElement : SnippetBoundElement
    {
        internal Func<string, string>? Function;

        public override string? ConvertText(string input)
        {
            return Function?.Invoke(input);
        }
    }
    [GeneratedRegex(@"\$\{([^\}]*)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
    [GeneratedRegex(@"^([a-zA-Z]+)\(([^\)]*)\)$", RegexOptions.CultureInvariant)]
    private static partial Regex FunctionPattern();
    [GeneratedRegex(@"\$\{([^\}]*)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
