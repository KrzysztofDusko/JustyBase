using System.Threading.Tasks;

namespace JustyBase.Editor.CompletionProviders;

public interface ICodeEditorCompletionProvider
{
    Task<CompletionResult> GetCompletionData(int position, char? triggerChar);
}
