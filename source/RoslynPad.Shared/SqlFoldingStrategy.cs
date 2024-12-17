using System;
using System.Collections.Generic;


namespace JustyBase.Editor.Folding;

public sealed class SqlFoldingStrategy
{
    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        IEnumerable<NewFolding> newFoldings = CreateNewFoldings(document, out int firstErrorOffset);
        manager.UpdateFoldings(newFoldings, firstErrorOffset);
    }

    /// <summary>
    /// Create <see cref="NewFolding"/>s for the specified document.
    /// </summary>
    public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
    {
        firstErrorOffset = -1;
        return CreateNewFoldings(document);
    }

    private const string START_WORD = "--region";
    private const string END_WORD = "--endregion";
    private const string REGION_WORD = "REGION ";
    /// <summary>
    /// Create <see cref="NewFolding"/>s for the specified document.
    /// </summary>
    public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
    {
        List<NewFolding> newFoldings = [];

        Stack<(int, string)> startOffsets = new();
        Span<char> charList = stackalloc char[128];

        for (int i = 0; i < document.TextLength - 1; i++)
        {
            //--REGION XXX
            //--ENDREGION
            if (document.GetCharAt(i) != '-' || document.GetCharAt(i + 1) != '-')
            {
                continue;
            }

            int spacesInWord = 0;
            bool start = true;
            bool end = true;
            bool fouded = false;
            int j = 2;
            for (; (j < START_WORD.Length || j < END_WORD.Length)
                && i + j + spacesInWord < document.TextLength && (start || end); j++)
            {
                var c1 = document.GetCharAt(i + j + spacesInWord) | 32;// R -> r
                if (c1 == ' ')
                {
                    spacesInWord++;
                    j--;
                    continue;
                }

                if (start && j < START_WORD.Length)
                {
                    if (c1 != START_WORD[j])
                    {
                        start = false;
                    }
                    else if (j == START_WORD.Length - 1)
                    {
                        fouded = true;
                        break;
                    }
                }
                if (end && j < END_WORD.Length)
                {
                    if (c1 != END_WORD[j])
                    {
                        end = false;
                    }
                    else if (j == END_WORD.Length - 1)
                    {
                        if (document.TextLength == i + j + spacesInWord + 1)
                        {
                            fouded = true;
                            break;
                        }
                        char c2 = document.GetCharAt(i + j + spacesInWord + 1);
                        if (c2 == '\r' || c2 == '\n')
                        {
                            fouded = true;
                            break;
                        }
                        if (c2 == ' ')
                        {
                            while (c2 != '\r' && c2 != '\n')
                            {
                                if (i + j + spacesInWord + 1 >= document.TextLength)
                                {
                                    break;
                                }
                                c2 = document.GetCharAt(i + j + spacesInWord + 1);
                                ++spacesInWord;
                            }
                            fouded = true;
                            break;
                        }
                    }
                }
            }

            if (start && fouded)
            {
                char c0 = '\0';
                int num = 1;
                do
                {
                    int nx = i + START_WORD.Length + num + spacesInWord;
                    if (nx > document.TextLength - 1)
                    {
                        break;
                    }
                    c0 = document.GetCharAt(i + START_WORD.Length + num + spacesInWord);
                    charList[num - 1] = c0;
                    ++num;
                } while (num < 128 && i + START_WORD.Length + num + spacesInWord < document.TextLength - 1
                && c0 != '\n' && c0 != '\r' /*&& c0 != ' '*/ && c0 != '\t');

                if (num >= 2)
                {
                    startOffsets.Push((i, REGION_WORD + charList[0..(num - 2)].ToString()));
                }
            }
            else if (end && fouded && startOffsets.Count > 0)
            {
                var (startOffset, regionName) = startOffsets.Pop();
                char c0 = document.GetCharAt((i + END_WORD.Length + spacesInWord - 1));
                if (c0 == '\r' || c0 == '\n')
                {
                    newFoldings.Add(new NewFolding(startOffset, i + END_WORD.Length + spacesInWord - 1) { Name = regionName });
                }
                else
                {
                    newFoldings.Add(new NewFolding(startOffset, i + END_WORD.Length + spacesInWord) { Name = regionName });
                }
            }
        }
        newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return newFoldings;
    }
}

//   public class BraceFoldingStrategy
//{
//	/// <summary>
//	/// Gets/Sets the opening brace. The default value is '{'.
//	/// </summary>
//	public char OpeningBrace { get; set; }

//	/// <summary>
//	/// Gets/Sets the closing brace. The default value is '}'.
//	/// </summary>
//	public char ClosingBrace { get; set; }

//	/// <summary>
//	/// Creates a new BraceFoldingStrategy.
//	/// </summary>
//	public BraceFoldingStrategy()
//	{
//		this.OpeningBrace = '{';
//		this.ClosingBrace = '}';
//	}

//	public void UpdateFoldings(FoldingManager manager, TextDocument document)
//	{
//		int firstErrorOffset;
//		IEnumerable<NewFolding> newFoldings = CreateNewFoldings(document, out firstErrorOffset);
//		manager.UpdateFoldings(newFoldings, firstErrorOffset);
//	}

//	/// <summary>
//	/// Create <see cref="NewFolding"/>s for the specified document.
//	/// </summary>
//	public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
//	{
//		firstErrorOffset = -1;
//		return CreateNewFoldings(document);
//	}

//	/// <summary>
//	/// Create <see cref="NewFolding"/>s for the specified document.
//	/// </summary>
//	public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
//	{
//		List<NewFolding> newFoldings = new List<NewFolding>();

//		Stack<int> startOffsets = new Stack<int>();
//		int lastNewLineOffset = 0;
//		char openingBrace = this.OpeningBrace;
//		char closingBrace = this.ClosingBrace;
//		for (int i = 0; i < document.TextLength; i++)
//		{
//			char c = document.GetCharAt(i);
//			if (c == openingBrace)
//			{
//				startOffsets.Push(i);
//			}
//			else if (c == closingBrace && startOffsets.Count > 0)
//			{
//				int startOffset = startOffsets.Pop();
//				// don't fold if opening and closing brace are on the same line
//				if (startOffset < lastNewLineOffset)
//				{
//					newFoldings.Add(new NewFolding(startOffset, i + 1));
//				}
//			}
//			else if (c == '\n' || c == '\r')
//			{
//				lastNewLineOffset = i + 1;
//			}
//		}
//		newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
//		return newFoldings;
//	}
//}
